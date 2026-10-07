namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task Start_InitializesDatabaseAndAdmin_ThenShowsLogin()
    {
        Mock<IDatabase> database = new();
        Mock<IAuthService> auth = new();
        MainWindowViewModel viewModel = new(database.Object, auth.Object, Mock.Of<IItemRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IThemeService>(), Mock.Of<IInputValidator>(), Mock.Of<IExcelExportService>());

        await viewModel.Start();

        database.Verify(x => x.Initialize(default), Times.Once);
        auth.Verify(x => x.EnsureAdminAccount(default), Times.Once);
        Assert.IsType<LoginViewModel>(viewModel.CurrentViewModel);
    }

    [Fact]
    public async Task SuccessfulLogin_NavigatesToInventory_AndLogoutReturnsToLogin()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(x => x.LoginWithBarcode("badge", default)).ReturnsAsync(true);
        Mock<IItemRepository> items = new();
        items.Setup(x => x.Query(null, null, null, null, default)).ReturnsAsync([]);
        items.Setup(x => x.GetCategories(default)).ReturnsAsync([]);
        items.Setup(x => x.GetInsertingUsers(default)).ReturnsAsync([]);
        MainWindowViewModel viewModel = new(Mock.Of<IDatabase>(), auth.Object, items.Object, Mock.Of<IUserRepository>(), Mock.Of<IThemeService>(), Mock.Of<IInputValidator>(), Mock.Of<IExcelExportService>());
        await viewModel.Start();

        LoginViewModel login = Assert.IsType<LoginViewModel>(viewModel.CurrentViewModel);
        login.BadgeInput = "badge";
        await login.BadgeLoginCommand.ExecuteAsync(null);

        InventoryViewModel inventory = Assert.IsType<InventoryViewModel>(viewModel.CurrentViewModel);
        await inventory.LogoutCommand.ExecuteAsync(null);

        Assert.IsType<LoginViewModel>(viewModel.CurrentViewModel);
        auth.Verify(x => x.Logout(), Times.Once);
    }

    [Fact]
    public async Task Inventory_CanNavigateToAccountAndUserManagement()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(x => x.LoginWithBarcode("badge", default)).ReturnsAsync(true);
        Mock<IItemRepository> items = new();
        items.Setup(x => x.Query(null, null, null, null, default)).ReturnsAsync([]);
        items.Setup(x => x.GetCategories(default)).ReturnsAsync([]);
        items.Setup(x => x.GetInsertingUsers(default)).ReturnsAsync([]);
        Mock<IUserRepository> users = new();
        users.Setup(x => x.GetAll(default)).ReturnsAsync([]);
        MainWindowViewModel viewModel = new(Mock.Of<IDatabase>(), auth.Object, items.Object, users.Object, Mock.Of<IThemeService>(), Mock.Of<IInputValidator>(), Mock.Of<IExcelExportService>());
        await viewModel.Start();
        LoginViewModel login = Assert.IsType<LoginViewModel>(viewModel.CurrentViewModel);
        login.BadgeInput = "badge";
        await login.BadgeLoginCommand.ExecuteAsync(null);

        await Assert.IsType<InventoryViewModel>(viewModel.CurrentViewModel).ShowAccountCommand.ExecuteAsync(null);
        Assert.IsType<AccountViewModel>(viewModel.CurrentViewModel);

        await Assert.IsType<AccountViewModel>(viewModel.CurrentViewModel).BackCommand.ExecuteAsync(null);
        await Assert.IsType<InventoryViewModel>(viewModel.CurrentViewModel).ShowUserManagementCommand.ExecuteAsync(null);
        Assert.IsType<UserManagementViewModel>(viewModel.CurrentViewModel);
    }
}
