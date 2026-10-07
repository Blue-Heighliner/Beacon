using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Artemis.Data;
using Artemis.Models;
using Artemis.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Artemis.ViewModels;

public partial class InventoryViewModel : ViewModelBase
{
    private const string AllFilter = "All";

    private readonly AuthService _auth;
    private readonly ItemRepository _items;
    private readonly Action _onLogout;
    private readonly Action _onShowAccount;
    private readonly Action _onShowUserManagement;
    private readonly DispatcherTimer _statusClearTimer;
    private bool _suppressRefresh;

    // Scan entry
    [ObservableProperty] private string _serialInput = "";
    [ObservableProperty] private string _itemNameInput = "";
    [ObservableProperty] private string _categoryInput = "";

    [ObservableProperty] private InventoryItem? _selectedItem;

    // Filters
    [ObservableProperty] private string _selectedCategoryFilter = AllFilter;
    [ObservableProperty] private string _selectedUserFilter = AllFilter;
    [ObservableProperty] private DateTimeOffset? _filterFrom;
    [ObservableProperty] private DateTimeOffset? _filterTo;

    // Status banner
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _statusIsError;

    public ObservableCollection<InventoryItem> Items { get; } = new();
    public ObservableCollection<string> CategoryFilterOptions { get; } = new() { AllFilter };
    public ObservableCollection<string> UserFilterOptions { get; } = new() { AllFilter };
    public ObservableCollection<string> CategorySuggestions { get; } = new();

    public string CurrentUsername => _auth.CurrentUser?.Username ?? "";

    public bool IsAdmin => _auth.CurrentUser?.IsAdmin ?? false;

    public InventoryViewModel(AuthService auth, ItemRepository items, Action onLogout,
        Action onShowAccount, Action onShowUserManagement)
    {
        _auth = auth;
        _items = items;
        _onLogout = onLogout;
        _onShowAccount = onShowAccount;
        _onShowUserManagement = onShowUserManagement;
        _statusClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        _statusClearTimer.Tick += (_, _) =>
        {
            StatusMessage = "";
            _statusClearTimer.Stop();
        };

        RefreshFilterOptions();
        RefreshItems();
    }

    [RelayCommand]
    private void AddItem()
    {
        var serial = SerialInput.Trim();
        if (serial.Length == 0)
            return;

        if (InputValidator.IsSuspicious(serial, ItemNameInput, CategoryInput))
        {
            SerialInput = "";
            ShowStatus(InputValidator.RejectionMessage, isError: true);
            return;
        }

        var added = _items.TryInsert(new InventoryItem
        {
            SerialNumber = serial,
            ItemName = ItemNameInput.Trim(),
            Category = CategoryInput.Trim(),
            InsertedBy = CurrentUsername,
            CreatedAt = DateTime.UtcNow,
        });

        // Clear only the serial so name/category persist for rapid batch scanning.
        SerialInput = "";

        if (added)
        {
            ShowStatus($"Added {serial}", isError: false);
            RefreshFilterOptions();
            RefreshItems();
        }
        else
        {
            ShowStatus($"Duplicate: {serial} already exists", isError: true);
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        _suppressRefresh = true;
        SelectedCategoryFilter = AllFilter;
        SelectedUserFilter = AllFilter;
        FilterFrom = null;
        FilterTo = null;
        _suppressRefresh = false;
        RefreshItems();
    }

    [RelayCommand]
    private void DeleteSelectedItem()
    {
        if (SelectedItem is not { } item)
            return;

        if (!IsAdmin && item.InsertedBy != CurrentUsername)
        {
            ShowStatus("You can only delete items you added.", isError: true);
            return;
        }

        _items.Delete(item.Id);
        ShowStatus($"Deleted {item.SerialNumber}", isError: false);
        SelectedItem = null;
        RefreshFilterOptions();
        RefreshItems();
    }

    [RelayCommand]
    private void Logout()
    {
        _auth.Logout();
        _onLogout();
    }

    [RelayCommand]
    private void ShowAccount() => _onShowAccount();

    [RelayCommand]
    private void ShowUserManagement() => _onShowUserManagement();

    [RelayCommand]
    private void ToggleTheme() => ThemeService.Toggle();

    /// <summary>Items matching the current filters, straight from the grid's collection.</summary>
    public List<InventoryItem> GetFilteredItems() => Items.ToList();

    public List<InventoryItem> GetAllItems() => _items.Query();

    public void NotifyExported(string filePath, int count) =>
        ShowStatus($"Exported {count} item(s) to {filePath}", isError: false);

    partial void OnSelectedCategoryFilterChanged(string value) => RefreshItems();
    partial void OnSelectedUserFilterChanged(string value) => RefreshItems();
    partial void OnFilterFromChanged(DateTimeOffset? value) => RefreshItems();
    partial void OnFilterToChanged(DateTimeOffset? value) => RefreshItems();

    private void RefreshItems()
    {
        if (_suppressRefresh)
            return;

        string? category = SelectedCategoryFilter == AllFilter ? null : SelectedCategoryFilter;
        string? user = SelectedUserFilter == AllFilter ? null : SelectedUserFilter;
        // Filter dates are local calendar days; convert to UTC bounds (to-date inclusive).
        DateTime? fromUtc = FilterFrom?.Date.ToUniversalTime();
        DateTime? toUtcExclusive = FilterTo?.Date.AddDays(1).ToUniversalTime();

        Items.Clear();
        foreach (var item in _items.Query(category, user, fromUtc, toUtcExclusive))
            Items.Add(item);
    }

    private void RefreshFilterOptions()
    {
        SyncOptions(CategoryFilterOptions, _items.GetCategories(), keepAllEntry: true);
        SyncOptions(UserFilterOptions, _items.GetInsertingUsers(), keepAllEntry: true);
        SyncOptions(CategorySuggestions, _items.GetCategories(), keepAllEntry: false);
    }

    private static void SyncOptions(ObservableCollection<string> target, List<string> values, bool keepAllEntry)
    {
        if (keepAllEntry)
            values.Insert(0, AllFilter);

        // Add/remove instead of Clear() so an active ComboBox selection isn't reset.
        foreach (var stale in target.Except(values).ToList())
            target.Remove(stale);
        foreach (var missing in values.Except(target).ToList())
            target.Add(missing);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
        _statusClearTimer.Stop();
        _statusClearTimer.Start();
    }
}
