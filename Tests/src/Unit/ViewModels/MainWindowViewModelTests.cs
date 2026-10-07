namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class MainWindowViewModelTests
{
    private readonly Mock<IDatabase> database = new();
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<INavigation> navigation = new();
    private readonly MainWindowViewModel viewModel;

    public MainWindowViewModelTests() => viewModel = new MainWindowViewModel(database.Object, auth.Object, navigation.Object);

    [Fact]
    public async Task Start_InitializesDatabaseAndAdmin_ThenShowsLogin()
    {
        await viewModel.Start();

        database.Verify(x => x.Initialize(default), Times.Once);
        auth.Verify(x => x.EnsureAdminAccount(default), Times.Once);
        navigation.Verify(x => x.ShowLogin(), Times.Once);
    }

    [Fact]
    public void CurrentViewModel_ReflectsNavigation_AndRaisesWhenItChanges()
    {
        LoginViewModel login = new(auth.Object, Mock.Of<IThemeService>(), navigation.Object);
        navigation.Setup(x => x.Current).Returns(login);
        List<string?> raised = [];
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        navigation.Raise(x => x.Changed += null);

        Assert.Same(login, viewModel.CurrentViewModel);
        Assert.Contains(nameof(MainWindowViewModel.CurrentViewModel), raised);
    }
}
