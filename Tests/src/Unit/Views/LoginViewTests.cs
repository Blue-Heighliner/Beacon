namespace BlueHeighliner.Beacon.Tests.Unit.Views;

public sealed class LoginViewTests
{
    private readonly Mock<IAuthService> auth = new();

    [AvaloniaFact]
    public void EnterInBadgeBox_RunsBadgeLogin()
    {
        auth.Setup(x => x.LoginWithBarcode("badge", default)).ReturnsAsync(true);
        (Window window, LoginView view) = Show();
        view.BadgeBox.Text = "badge";
        view.BadgeBox.Focus();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

        auth.Verify(x => x.LoginWithBarcode("badge", default), Times.Once);
    }

    [AvaloniaFact]
    public void EnterInPasswordBox_RunsLogin()
    {
        auth.Setup(x => x.LoginWithPassword("bob", "pw", default)).ReturnsAsync(true);
        (Window window, LoginView view) = Show();
        view.PasswordBox.Text = "pw";
        ((LoginViewModel)view.DataContext!).Username = "bob";
        view.PasswordBox.Focus();

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

        auth.Verify(x => x.LoginWithPassword("bob", "pw", default), Times.Once);
    }

    [AvaloniaFact]
    public void OtherKeys_DoNothing()
    {
        (Window window, LoginView view) = Show();
        view.BadgeBox.Text = "badge";
        view.BadgeBox.Focus();

        window.KeyPress(Key.A, RawInputModifiers.None, PhysicalKey.A, "a");

        auth.Verify(x => x.LoginWithBarcode(It.IsAny<string>(), default), Times.Never);
    }

    [AvaloniaFact]
    public void Loaded_FocusesBadgeBox()
    {
        (_, LoginView view) = Show();

        Assert.True(view.BadgeBox.IsFocused);
    }

    private (Window Window, LoginView View) Show()
    {
        LoginView view = new() { DataContext = new LoginViewModel(auth.Object, Mock.Of<IThemeService>(), Mock.Of<INavigation>()) };
        Window window = new() { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, view);
    }
}
