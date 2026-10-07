namespace BlueHeighliner.Beacon.Tests.Unit.Views;

public sealed class SimpleViewTests
{
    [AvaloniaFact]
    public void AccountView_Constructs() => Assert.NotNull(new AccountView());

    [AvaloniaFact]
    public async Task MainWindow_StartsItsViewModelWhenOpened()
    {
        Mock<IDatabase> database = new();
        Mock<INavigation> navigation = new();
        MainWindowViewModel viewModel = new(database.Object, Mock.Of<IAuthService>(), navigation.Object);
        MainWindow window = new() { DataContext = viewModel };

        window.Show();
        await Task.Delay(100);

        database.Verify(x => x.Initialize(default), Times.Once);
        navigation.Verify(x => x.ShowLogin(), Times.Once);
    }

    [AvaloniaFact]
    public void MainWindow_OpenedWithoutViewModel_DoesNothing()
    {
        MainWindow window = new();

        window.Show();

        Assert.Null(window.DataContext);
    }

    [AvaloniaFact]
    public void UserManagementView_ConfirmDelete_DeletesSelectedUser()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(x => x.AdminDeleteUser(It.IsAny<User>(), default)).ReturnsAsync((true, ""));
        Mock<IUserRepository> users = new();
        User target = new() { Id = 5, Username = "target", DodId = "1234567890", CreatedAt = DateTime.UtcNow };
        users.Setup(x => x.GetAll(default)).ReturnsAsync([target]);
        UserManagementViewModel viewModel = new(auth.Object, users.Object, Mock.Of<INavigation>());
        viewModel.Load().GetAwaiter().GetResult();
        viewModel.SelectedUser = target;
        UserManagementView view = new() { DataContext = viewModel };
        Window window = new() { Content = view };
        window.Show();

        view.ConfirmDeleteUserButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        auth.Verify(x => x.AdminDeleteUser(target, default), Times.Once);
    }

    [AvaloniaFact]
    public void UserManagementView_ConfirmDelete_WithoutViewModel_DoesNothing()
    {
        UserManagementView view = new();
        Window window = new() { Content = view };
        window.Show();

        view.ConfirmDeleteUserButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Null(view.DataContext);
    }
}
