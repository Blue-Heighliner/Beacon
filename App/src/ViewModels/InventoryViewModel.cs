namespace BlueHeighliner.Beacon.ViewModels;

/// <summary>Scan entry, filtering, and deletion of inventory items.</summary>
internal sealed partial class InventoryViewModel(IAuthService auth, IItemRepository items, IThemeService theme, IInputValidator validator, IExcelExportService excel, IFilePicker filePicker, IClock clock, IUiTimerFactory timers, INavigation navigation) : ViewModelBase, ILoadable
{
    private static readonly string allFilter = "All";

    private IUiTimer? statusClearTimer;

    private bool suppressRefresh;

    private int refreshGeneration;

    [ObservableProperty]
    private string serialInput = "";

    [ObservableProperty]
    private string itemNameInput = "";

    [ObservableProperty]
    private string categoryInput = "";

    [ObservableProperty]
    private InventoryItem? selectedItem;

    [ObservableProperty]
    private string selectedCategoryFilter = allFilter;

    [ObservableProperty]
    private string selectedUserFilter = allFilter;

    [ObservableProperty]
    private DateTimeOffset? filterFrom;

    [ObservableProperty]
    private DateTimeOffset? filterTo;

    [ObservableProperty]
    private string statusMessage = "";

    [ObservableProperty]
    private bool statusIsError;

    /// <summary>Gets the items matching the current filters.</summary>
    public ObservableCollection<InventoryItem> Items { get; } = [];

    /// <summary>Gets the category filter choices, including the "All" entry.</summary>
    public ObservableCollection<string> CategoryFilterOptions { get; } = [allFilter];

    /// <summary>Gets the inserting-user filter choices, including the "All" entry.</summary>
    public ObservableCollection<string> UserFilterOptions { get; } = [allFilter];

    /// <summary>Gets the category suggestions for the entry box.</summary>
    public ObservableCollection<string> CategorySuggestions { get; } = [];

    /// <summary>Gets the signed-in user's username.</summary>
    public string CurrentUsername => auth.CurrentUser?.Username ?? "";

    /// <summary>Gets a value indicating whether the signed-in user is an administrator.</summary>
    public bool IsAdmin => auth.CurrentUser?.IsAdmin ?? false;

    /// <summary>Loads the filter choices and the item list.</summary>
    public async Task Load()
    {
        await RefreshFilterOptions();
        await RefreshItems();
    }

    partial void OnSelectedCategoryFilterChanged(string value) => RefreshItemsCommand.Execute(null);

    partial void OnSelectedUserFilterChanged(string value) => RefreshItemsCommand.Execute(null);

    partial void OnFilterFromChanged(DateTimeOffset? value) => RefreshItemsCommand.Execute(null);

    partial void OnFilterToChanged(DateTimeOffset? value) => RefreshItemsCommand.Execute(null);

    [RelayCommand]
    private async Task AddItem()
    {
        string serial = SerialInput.Trim();
        if (serial.Length == 0)
        {
            return;
        }

        if (validator.IsSuspicious(serial, ItemNameInput, CategoryInput))
        {
            SerialInput = "";
            ShowStatus(validator.RejectionMessage, isError: true);
            return;
        }

        bool added = await items.TryInsert(new InventoryItem
        {
            SerialNumber = serial,
            ItemName = ItemNameInput.Trim(),
            Category = CategoryInput.Trim(),
            InsertedBy = CurrentUsername,
            CreatedAt = clock.UtcNow,
        });

        // Clear only the serial so name/category persist for rapid batch scanning.
        SerialInput = "";

        if (added)
        {
            ShowStatus($"Added {serial}", isError: false);
            await RefreshFilterOptions();
            await RefreshItems();
        }
        else
        {
            ShowStatus($"Duplicate: {serial} already exists", isError: true);
        }
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        suppressRefresh = true;
        SelectedCategoryFilter = allFilter;
        SelectedUserFilter = allFilter;
        FilterFrom = null;
        FilterTo = null;
        suppressRefresh = false;
        await RefreshItems();
    }

    [RelayCommand]
    private async Task DeleteSelectedItem()
    {
        if (SelectedItem is not { } item)
        {
            return;
        }

        if (!IsAdmin && item.InsertedBy != CurrentUsername)
        {
            ShowStatus("You can only delete items you added.", isError: true);
            return;
        }

        await items.Delete(item.Id);
        ShowStatus($"Deleted {item.SerialNumber}", isError: false);
        SelectedItem = null;
        await RefreshFilterOptions();
        await RefreshItems();
    }

    [RelayCommand]
    private Task Logout()
    {
        auth.Logout();
        return navigation.ShowLogin();
    }

    [RelayCommand]
    private Task ShowAccount() => navigation.ShowAccount();

    [RelayCommand]
    private Task ShowUserManagement() => navigation.ShowUserManagement();

    [RelayCommand]
    private Task ExportFiltered() => Export(filteredOnly: true);

    [RelayCommand]
    private Task ExportAll() => Export(filteredOnly: false);

    [RelayCommand]
    private void ToggleTheme() => theme.Toggle();

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task RefreshItems()
    {
        if (suppressRefresh)
        {
            return;
        }

        string? category = SelectedCategoryFilter == allFilter ? null : SelectedCategoryFilter;
        string? user = SelectedUserFilter == allFilter ? null : SelectedUserFilter;

        // Filter dates are local calendar days; convert to UTC bounds (to-date inclusive).
        DateTime? fromUtc = FilterFrom?.Date.ToUniversalTime();
        DateTime? toUtcExclusive = FilterTo?.Date.AddDays(1).ToUniversalTime();

        int generation = ++refreshGeneration;
        List<InventoryItem> loaded = await items.Query(category, user, fromUtc, toUtcExclusive);
        if (generation != refreshGeneration)
        {
            return;
        }

        Items.Clear();
        foreach (InventoryItem item in loaded)
        {
            Items.Add(item);
        }
    }

    private async Task RefreshFilterOptions()
    {
        List<string> categories = await items.GetCategories();
        List<string> users = await items.GetInsertingUsers();
        SyncOptions(CategoryFilterOptions, [allFilter, .. categories]);
        SyncOptions(UserFilterOptions, [allFilter, .. users]);
        SyncOptions(CategorySuggestions, categories);
    }

    private void SyncOptions(ObservableCollection<string> target, List<string> values)
    {
        // Add/remove instead of Clear() so an active ComboBox selection isn't reset.
        foreach (string stale in target.Except(values).ToList())
        {
            target.Remove(stale);
        }

        foreach (string missing in values.Except(target).ToList())
        {
            target.Add(missing);
        }
    }

    private async Task Export(bool filteredOnly)
    {
        string? filePath = await filePicker.PickExcelSavePath($"Inventory-{clock.UtcNow.ToLocalTime():yyyy-MM-dd}.xlsx");
        if (filePath is null)
        {
            return;
        }

        List<InventoryItem> exported = filteredOnly ? [.. Items] : await items.Query();
        await excel.Export(filePath, exported);
        ShowStatus($"Exported {exported.Count} item(s) to {filePath}", isError: false);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
        statusClearTimer ??= timers.Create(TimeSpan.FromSeconds(4), () => StatusMessage = "");
        statusClearTimer.Restart();
    }
}
