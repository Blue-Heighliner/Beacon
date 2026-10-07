namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class AccountViewModelTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<INavigation> navigation = new();
    private readonly AccountViewModel viewModel;

    public AccountViewModelTests()
    {
        auth.Setup(x => x.CurrentUser).Returns(new User { Id = 1, Username = "bob", DodId = "1234567890", CreatedAt = DateTime.UtcNow });
        viewModel = new AccountViewModel(auth.Object, navigation.Object);
    }

    [Fact]
    public void Properties_ReflectCurrentUser()
    {
        Assert.Equal("bob", viewModel.Username);
        Assert.Equal("1234567890", viewModel.DodId);
        Assert.False(viewModel.HasBadge);
    }

    [Fact]
    public async Task ChangePassword_WithMismatch_ShowsError()
    {
        viewModel.NewPassword = "one";
        viewModel.NewPasswordConfirm = "two";

        await viewModel.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("New passwords do not match.", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task ChangePassword_Failure_ShowsServiceError()
    {
        auth.Setup(x => x.ChangePassword(It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync((false, "bad"));

        await viewModel.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("bad", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ChangePassword_Success_ClearsFields()
    {
        auth.Setup(x => x.ChangePassword("old", "newsecret", default)).ReturnsAsync((true, ""));
        viewModel.CurrentPassword = "old";
        viewModel.NewPassword = "newsecret";
        viewModel.NewPasswordConfirm = "newsecret";

        await viewModel.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("Password changed.", viewModel.StatusMessage);
        Assert.Equal("", viewModel.CurrentPassword);
        Assert.Equal("", viewModel.NewPassword);
    }

    [Fact]
    public async Task ChangeBadge_Success_ClearsInputAndRaisesHasBadge()
    {
        auth.Setup(x => x.ChangeBadge("badge", default)).ReturnsAsync((true, ""));
        viewModel.BadgeInput = "badge";
        List<string?> raised = [];
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        await viewModel.ChangeBadgeCommand.ExecuteAsync(null);

        Assert.Equal("", viewModel.BadgeInput);
        Assert.Contains(nameof(AccountViewModel.HasBadge), raised);
        Assert.Equal("Badge updated.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ChangeBadge_Failure_ShowsError()
    {
        auth.Setup(x => x.ChangeBadge(It.IsAny<string>(), default)).ReturnsAsync((false, "bad badge"));

        await viewModel.ChangeBadgeCommand.ExecuteAsync(null);

        Assert.Equal("bad badge", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task Back_InvokesCallback()
    {
        await viewModel.BackCommand.ExecuteAsync(null);

        navigation.Verify(x => x.ShowInventory(), Times.Once);
    }
}
