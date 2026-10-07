namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class LoginViewModelTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IThemeService> theme = new();
    private readonly Mock<INavigation> navigation = new();
    private readonly LoginViewModel viewModel;
    private int signedIn;

    public LoginViewModelTests()
    {
        navigation.Setup(x => x.ShowInventory()).Returns(() =>
        {
            signedIn++;
            return Task.CompletedTask;
        });
        viewModel = new LoginViewModel(auth.Object, theme.Object, navigation.Object);
    }

    [Fact]
    public async Task Login_WithMissingFields_ShowsError()
    {
        await viewModel.LoginCommand.ExecuteAsync(null);

        Assert.True(viewModel.StatusIsError);
        Assert.Equal(0, signedIn);
        auth.Verify(x => x.LoginWithPassword(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Navigates()
    {
        auth.Setup(x => x.LoginWithPassword("bob", "pw", default)).ReturnsAsync(true);
        viewModel.Username = "bob";
        viewModel.Password = "pw";

        await viewModel.LoginCommand.ExecuteAsync(null);

        Assert.Equal(1, signedIn);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        viewModel.Username = "bob";
        viewModel.Password = "pw";

        await viewModel.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Invalid username or password.", viewModel.StatusMessage);
        Assert.Equal(0, signedIn);
    }

    [Fact]
    public async Task BadgeLogin_WithKnownBadge_Navigates()
    {
        auth.Setup(x => x.LoginWithBarcode("badge", default)).ReturnsAsync(true);
        viewModel.BadgeInput = "badge";

        await viewModel.BadgeLoginCommand.ExecuteAsync(null);

        Assert.Equal(1, signedIn);
    }

    [Fact]
    public async Task BadgeLogin_WithUnknownBadge_ClearsInputAndShowsError()
    {
        viewModel.BadgeInput = "badge";

        await viewModel.BadgeLoginCommand.ExecuteAsync(null);

        Assert.Equal("", viewModel.BadgeInput);
        Assert.Equal("Badge not recognized.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task BadgeLogin_WithBlankInput_DoesNothing()
    {
        await viewModel.BadgeLoginCommand.ExecuteAsync(null);

        auth.Verify(x => x.LoginWithBarcode(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShowsError()
    {
        viewModel.RegPassword = "one";
        viewModel.RegPasswordConfirm = "two";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Passwords do not match.", viewModel.StatusMessage);
        auth.Verify(x => x.Register(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Register_Failure_ShowsServiceError()
    {
        auth.Setup(x => x.Register(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string>(), default)).ReturnsAsync((false, "nope"));

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("nope", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task Register_Success_PrefillsUsernameAndClearsForm()
    {
        auth.Setup(x => x.Register(" bob ", "pw", "badge", "1234567890", default)).ReturnsAsync((true, ""));
        viewModel.RegUsername = " bob ";
        viewModel.RegPassword = "pw";
        viewModel.RegPasswordConfirm = "pw";
        viewModel.RegBadge = "badge";
        viewModel.RegDodId = "1234567890";

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.False(viewModel.StatusIsError);
        Assert.Equal("bob", viewModel.Username);
        Assert.Equal("", viewModel.RegUsername);
        Assert.Equal("", viewModel.RegPassword);
        Assert.Equal("", viewModel.RegDodId);
    }

    [Fact]
    public void ToggleTheme_DelegatesToThemeService()
    {
        viewModel.ToggleThemeCommand.Execute(null);

        theme.Verify(x => x.Toggle(), Times.Once);
    }
}
