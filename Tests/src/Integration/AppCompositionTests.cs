namespace BlueHeighliner.Beacon.Tests.Integration;

public sealed class AppCompositionTests
{
    [Fact]
    public async Task AddBeacon_ComposesTheWholeApp_AndAdminCanSignIn()
    {
        using TempDirectory directory = new();
        await using ServiceProvider provider = new ServiceCollection().AddBeacon(directory.Path).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        MainWindowViewModel main = provider.GetRequiredService<MainWindowViewModel>();

        await main.Start();

        LoginViewModel login = Assert.IsType<LoginViewModel>(main.CurrentViewModel);
        login.Username = "admin";
        login.Password = "TestAdmin123";
        await login.LoginCommand.ExecuteAsync(null);
        InventoryViewModel inventory = Assert.IsType<InventoryViewModel>(main.CurrentViewModel);
        Assert.True(inventory.IsAdmin);
        Assert.True(File.Exists(Path.Combine(directory.Path, "inventory.db")));
    }

    [Fact]
    public void AddBeacon_RegistersTheSingletonsOnce()
    {
        using TempDirectory directory = new();
        using ServiceProvider provider = new ServiceCollection().AddBeacon(directory.Path).BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<INavigation>(), provider.GetRequiredService<INavigation>());
        Assert.Same(provider.GetRequiredService<IAuthService>(), provider.GetRequiredService<IAuthService>());
        Assert.NotSame(provider.GetRequiredService<AccountViewModel>(), provider.GetRequiredService<AccountViewModel>());
        Assert.Equal(directory.Path, provider.GetRequiredService<IAppPaths>().DataDirectory);
    }

    [Fact]
    public void CreateServices_StoresDataUnderTheLocalApplicationDataFolder()
    {
        using ServiceProvider provider = App.CreateServices();

        Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Beacon"), provider.GetRequiredService<IAppPaths>().DataDirectory);
    }
}
