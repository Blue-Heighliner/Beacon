namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class NavigationTests
{
    private readonly Mock<IServiceProvider> services = new();
    private readonly Mock<IItemRepository> items = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly Navigation navigation;

    public NavigationTests()
    {
        items.Setup(x => x.Query(null, null, null, null, default)).ReturnsAsync([]);
        items.Setup(x => x.GetCategories(default)).ReturnsAsync([]);
        items.Setup(x => x.GetInsertingUsers(default)).ReturnsAsync([]);
        users.Setup(x => x.GetAll(default)).ReturnsAsync([]);
        navigation = new Navigation(services.Object);
    }

    [Fact]
    public void Current_StartsNull() => Assert.Null(navigation.Current);

    [Fact]
    public async Task ShowLogin_ResolvesTheViewModelAndNotifies()
    {
        LoginViewModel login = new(Mock.Of<IAuthService>(), Mock.Of<IThemeService>(), navigation);
        services.Setup(x => x.GetService(typeof(LoginViewModel))).Returns(login);
        int changes = 0;
        navigation.Changed += () => changes++;

        await navigation.ShowLogin();

        Assert.Same(login, navigation.Current);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task ShowAccount_ResolvesTheAccountViewModel()
    {
        AccountViewModel account = new(Mock.Of<IAuthService>(), navigation);
        services.Setup(x => x.GetService(typeof(AccountViewModel))).Returns(account);

        await navigation.ShowAccount();

        Assert.Same(account, navigation.Current);
    }

    [Fact]
    public async Task ShowInventory_LoadsTheViewModelBeforePublishingIt()
    {
        InventoryViewModel inventory = new(Mock.Of<IAuthService>(), items.Object, Mock.Of<IThemeService>(), Mock.Of<IInputValidator>(), Mock.Of<IExcelExportService>(), Mock.Of<IFilePicker>(), Mock.Of<IClock>(), Mock.Of<IUiTimerFactory>(), navigation);
        services.Setup(x => x.GetService(typeof(InventoryViewModel))).Returns(inventory);
        bool loadedBeforePublish = false;
        navigation.Changed += () => loadedBeforePublish = items.Invocations.Any(i => i.Method.Name == nameof(IItemRepository.Query));

        await navigation.ShowInventory();

        Assert.True(loadedBeforePublish);
        Assert.Same(inventory, navigation.Current);
    }

    [Fact]
    public async Task ShowUserManagement_LoadsTheUserList()
    {
        UserManagementViewModel management = new(Mock.Of<IAuthService>(), users.Object, navigation);
        services.Setup(x => x.GetService(typeof(UserManagementViewModel))).Returns(management);

        await navigation.ShowUserManagement();

        users.Verify(x => x.GetAll(default), Times.Once);
        Assert.Same(management, navigation.Current);
    }

    [Fact]
    public async Task Show_WithoutRegisteredViewModel_Throws() => await Assert.ThrowsAsync<InvalidOperationException>(navigation.ShowInventory);
}
