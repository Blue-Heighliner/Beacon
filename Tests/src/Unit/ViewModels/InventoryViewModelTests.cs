namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class InventoryViewModelTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IItemRepository> items = new();
    private readonly Mock<IThemeService> theme = new();
    private readonly Mock<IInputValidator> validator = new();
    private readonly Mock<IExcelExportService> excel = new();
    private readonly Mock<IFilePicker> picker = new();
    private readonly Mock<IClock> clock = new();
    private readonly Mock<IUiTimerFactory> timers = new();
    private readonly Mock<IUiTimer> timer = new();
    private readonly Mock<INavigation> navigation = new();
    private readonly DateTime now = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
    private Action? onTick;
    private readonly InventoryItem existing = new() { Id = 3, SerialNumber = "SN1", ItemName = "Radio", Category = "Comms", InsertedBy = "bob", CreatedAt = DateTime.UtcNow };
    private readonly InventoryViewModel viewModel;

    public InventoryViewModelTests()
    {
        auth.Setup(x => x.CurrentUser).Returns(new User { Id = 1, Username = "bob", DodId = "1234567890", CreatedAt = DateTime.UtcNow });
        validator.Setup(x => x.RejectionMessage).Returns("rejected");
        items.Setup(x => x.Query(null, null, null, null, default)).ReturnsAsync([existing]);
        items.Setup(x => x.GetCategories(default)).ReturnsAsync(["Comms"]);
        items.Setup(x => x.GetInsertingUsers(default)).ReturnsAsync(["bob"]);
        items.Setup(x => x.TryInsert(It.IsAny<InventoryItem>(), default)).ReturnsAsync(true);
        clock.Setup(x => x.UtcNow).Returns(now);
        timers.Setup(x => x.Create(TimeSpan.FromSeconds(4), It.IsAny<Action>())).Returns((TimeSpan _, Action tick) =>
        {
            onTick = tick;
            return timer.Object;
        });
        viewModel = new InventoryViewModel(auth.Object, items.Object, theme.Object, validator.Object, excel.Object, picker.Object, clock.Object, timers.Object, navigation.Object);
    }

    [Fact]
    public async Task Load_PopulatesItemsAndFilterOptions()
    {
        await viewModel.Load();

        Assert.Equal([existing], viewModel.Items);
        Assert.Equal(["All", "Comms"], viewModel.CategoryFilterOptions);
        Assert.Equal(["All", "bob"], viewModel.UserFilterOptions);
        Assert.Equal(["Comms"], viewModel.CategorySuggestions);
        Assert.Equal("bob", viewModel.CurrentUsername);
        Assert.False(viewModel.IsAdmin);
    }

    [Fact]
    public async Task AddItem_WithBlankSerial_DoesNothing()
    {
        await viewModel.AddItemCommand.ExecuteAsync(null);

        items.Verify(x => x.TryInsert(It.IsAny<InventoryItem>(), default), Times.Never);
    }

    [Fact]
    public async Task AddItem_WithSuspiciousInput_IsRejected()
    {
        validator.Setup(x => x.IsSuspicious(It.IsAny<string?[]>())).Returns(true);
        viewModel.SerialInput = "a;b";

        await viewModel.AddItemCommand.ExecuteAsync(null);

        Assert.Equal("rejected", viewModel.StatusMessage);
        Assert.Equal("", viewModel.SerialInput);
        items.Verify(x => x.TryInsert(It.IsAny<InventoryItem>(), default), Times.Never);
    }

    [Fact]
    public async Task AddItem_Success_InsertsAndKeepsNameAndCategory()
    {
        viewModel.SerialInput = " SN2 ";
        viewModel.ItemNameInput = " Laptop ";
        viewModel.CategoryInput = " IT ";

        await viewModel.AddItemCommand.ExecuteAsync(null);

        items.Verify(x => x.TryInsert(It.Is<InventoryItem>(i => i.SerialNumber == "SN2" && i.ItemName == "Laptop" && i.Category == "IT" && i.InsertedBy == "bob" && i.CreatedAt == now), default), Times.Once);
        Assert.Equal("Added SN2", viewModel.StatusMessage);
        Assert.Equal("", viewModel.SerialInput);
        Assert.Equal(" Laptop ", viewModel.ItemNameInput);
    }

    [Fact]
    public async Task AddItem_Duplicate_ShowsError()
    {
        items.Setup(x => x.TryInsert(It.IsAny<InventoryItem>(), default)).ReturnsAsync(false);
        viewModel.SerialInput = "SN1";

        await viewModel.AddItemCommand.ExecuteAsync(null);

        Assert.Equal("Duplicate: SN1 already exists", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task DeleteSelectedItem_ByAnotherUser_IsRefusedForNonAdmins()
    {
        await viewModel.Load();
        viewModel.SelectedItem = existing with { InsertedBy = "someone" };

        await viewModel.DeleteSelectedItemCommand.ExecuteAsync(null);

        Assert.Equal("You can only delete items you added.", viewModel.StatusMessage);
        items.Verify(x => x.Delete(It.IsAny<long>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteSelectedItem_OwnItem_Deletes()
    {
        await viewModel.Load();
        viewModel.SelectedItem = existing;

        await viewModel.DeleteSelectedItemCommand.ExecuteAsync(null);

        items.Verify(x => x.Delete(3, default), Times.Once);
        Assert.Null(viewModel.SelectedItem);
    }

    [Fact]
    public async Task DeleteSelectedItem_WithoutSelection_DoesNothing()
    {
        await viewModel.DeleteSelectedItemCommand.ExecuteAsync(null);

        items.Verify(x => x.Delete(It.IsAny<long>(), default), Times.Never);
    }

    [Fact]
    public async Task ChangingCategoryFilter_RequeriesWithFilter()
    {
        await viewModel.Load();

        viewModel.SelectedCategoryFilter = "Comms";
        await Task.Yield();

        items.Verify(x => x.Query("Comms", null, null, null, default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ClearFilters_ResetsEverything()
    {
        await viewModel.Load();
        viewModel.SelectedCategoryFilter = "Comms";
        viewModel.SelectedUserFilter = "bob";

        await viewModel.ClearFiltersCommand.ExecuteAsync(null);

        Assert.Equal("All", viewModel.SelectedCategoryFilter);
        Assert.Equal("All", viewModel.SelectedUserFilter);
        Assert.Null(viewModel.FilterFrom);
    }

    [Fact]
    public async Task Logout_SignsOutThenNavigates()
    {
        await viewModel.LogoutCommand.ExecuteAsync(null);

        auth.Verify(x => x.Logout(), Times.Once);
        navigation.Verify(x => x.ShowLogin(), Times.Once);
    }

    [Fact]
    public void ToggleTheme_DelegatesToThemeService()
    {
        viewModel.ToggleThemeCommand.Execute(null);

        theme.Verify(x => x.Toggle(), Times.Once);
    }

    [Fact]
    public async Task ShowAccountAndUserManagement_Navigate()
    {
        await viewModel.ShowAccountCommand.ExecuteAsync(null);
        await viewModel.ShowUserManagementCommand.ExecuteAsync(null);

        navigation.Verify(x => x.ShowAccount(), Times.Once);
        navigation.Verify(x => x.ShowUserManagement(), Times.Once);
    }

    [Fact]
    public async Task ExportFiltered_UsesGridItemsAndSuggestsADatedFileName()
    {
        picker.Setup(x => x.PickExcelSavePath($"Inventory-{now.ToLocalTime():yyyy-MM-dd}.xlsx")).ReturnsAsync("out.xlsx");
        await viewModel.Load();

        await viewModel.ExportFilteredCommand.ExecuteAsync(null);

        excel.Verify(x => x.Export("out.xlsx", It.Is<IEnumerable<InventoryItem>>(e => e.Single() == existing), default), Times.Once);
        Assert.Equal("Exported 1 item(s) to out.xlsx", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ExportAll_QueriesEveryItem()
    {
        picker.Setup(x => x.PickExcelSavePath(It.IsAny<string>())).ReturnsAsync("all.xlsx");

        await viewModel.ExportAllCommand.ExecuteAsync(null);

        items.Verify(x => x.Query(null, null, null, null, default), Times.Once);
        excel.Verify(x => x.Export("all.xlsx", It.IsAny<IEnumerable<InventoryItem>>(), default), Times.Once);
    }

    [Fact]
    public async Task Export_WhenPickerCancelled_DoesNothing()
    {
        await viewModel.ExportAllCommand.ExecuteAsync(null);

        excel.Verify(x => x.Export(It.IsAny<string>(), It.IsAny<IEnumerable<InventoryItem>>(), default), Times.Never);
        Assert.Equal("", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StatusBanner_ClearsWhenTheTimerTicks()
    {
        viewModel.SerialInput = "SN2";
        await viewModel.AddItemCommand.ExecuteAsync(null);
        Assert.Equal("Added SN2", viewModel.StatusMessage);

        onTick!();

        Assert.Equal("", viewModel.StatusMessage);
        timer.Verify(x => x.Restart(), Times.Once);
    }

    [Fact]
    public async Task StatusBanner_ReusesOneTimerAcrossMessages()
    {
        viewModel.SerialInput = "SN2";
        await viewModel.AddItemCommand.ExecuteAsync(null);
        viewModel.SerialInput = "SN3";
        await viewModel.AddItemCommand.ExecuteAsync(null);

        timers.Verify(x => x.Create(It.IsAny<TimeSpan>(), It.IsAny<Action>()), Times.Once);
        timer.Verify(x => x.Restart(), Times.Exactly(2));
    }
}
