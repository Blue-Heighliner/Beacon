namespace BlueHeighliner.Beacon.Tests.Unit.ViewModels;

public sealed class UserManagementViewModelTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly User alice = new() { Id = 1, Username = "alice", DodId = "1111111111", CreatedAt = DateTime.UtcNow };
    private readonly User root = new() { Id = 2, Username = "root", DodId = "2222222222", IsAdmin = true, CreatedAt = DateTime.UtcNow };
    private readonly Mock<INavigation> navigation = new();
    private readonly UserManagementViewModel viewModel;

    public UserManagementViewModelTests()
    {
        users.Setup(x => x.GetAll(default)).ReturnsAsync([alice, root]);
        viewModel = new UserManagementViewModel(auth.Object, users.Object, navigation.Object);
    }

    [Fact]
    public async Task Load_PopulatesUsers()
    {
        await viewModel.Load();

        Assert.Equal([alice, root], viewModel.Users);
        Assert.False(viewModel.HasSelection);
    }

    [Fact]
    public async Task SelectingUser_FillsEditFields_AndGatesDeletion()
    {
        await viewModel.Load();

        viewModel.SelectedUser = alice;
        Assert.Equal("alice", viewModel.EditUsername);
        Assert.Equal("1111111111", viewModel.EditDodId);
        Assert.True(viewModel.HasSelection);
        Assert.True(viewModel.CanDeleteSelected);

        viewModel.SelectedUser = root;
        Assert.False(viewModel.CanDeleteSelected);
    }

    [Fact]
    public async Task SaveUserInfo_Success_RefreshesAndKeepsSelection()
    {
        auth.Setup(x => x.AdminUpdateUser(alice, "alice2", "1111111111", default)).ReturnsAsync((true, ""));
        await viewModel.Load();
        viewModel.SelectedUser = alice;
        viewModel.EditUsername = "alice2";

        await viewModel.SaveUserInfoCommand.ExecuteAsync(null);

        Assert.Equal("Updated 'alice'.", viewModel.StatusMessage);
        Assert.Equal(alice, viewModel.SelectedUser);
    }

    [Fact]
    public async Task SaveUserInfo_Failure_ShowsError()
    {
        auth.Setup(x => x.AdminUpdateUser(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), default)).ReturnsAsync((false, "denied"));
        await viewModel.Load();
        viewModel.SelectedUser = alice;

        await viewModel.SaveUserInfoCommand.ExecuteAsync(null);

        Assert.Equal("denied", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task SaveUserInfo_WithoutSelection_DoesNothing()
    {
        await viewModel.SaveUserInfoCommand.ExecuteAsync(null);

        auth.Verify(x => x.AdminUpdateUser(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ApplyPasswordReset_ClearsInputAndReportsResult()
    {
        auth.Setup(x => x.AdminResetPassword(alice, "newsecret", default)).ReturnsAsync((true, ""));
        await viewModel.Load();
        viewModel.SelectedUser = alice;
        viewModel.ResetPassword = "newsecret";

        await viewModel.ApplyPasswordResetCommand.ExecuteAsync(null);

        Assert.Equal("", viewModel.ResetPassword);
        Assert.Equal("Password reset for 'alice'.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ApplyBadgeReset_Failure_ShowsError()
    {
        auth.Setup(x => x.AdminResetBadge(alice, "badge", default)).ReturnsAsync((false, "taken"));
        await viewModel.Load();
        viewModel.SelectedUser = alice;
        viewModel.ResetBadge = "badge";

        await viewModel.ApplyBadgeResetCommand.ExecuteAsync(null);

        Assert.Equal("taken", viewModel.StatusMessage);
        Assert.Equal("", viewModel.ResetBadge);
    }

    [Fact]
    public async Task DeleteSelectedUser_Success_ReloadsAndClearsSelection()
    {
        auth.Setup(x => x.AdminDeleteUser(alice, default)).ReturnsAsync((true, ""));
        await viewModel.Load();
        viewModel.SelectedUser = alice;

        await viewModel.DeleteSelectedUserCommand.ExecuteAsync(null);

        Assert.Equal("Deleted user 'alice'.", viewModel.StatusMessage);
        Assert.Null(viewModel.SelectedUser);
    }

    [Fact]
    public async Task DeleteSelectedUser_Failure_ShowsError()
    {
        auth.Setup(x => x.AdminDeleteUser(It.IsAny<User>(), default)).ReturnsAsync((false, "no"));
        await viewModel.Load();
        viewModel.SelectedUser = root;

        await viewModel.DeleteSelectedUserCommand.ExecuteAsync(null);

        Assert.Equal("no", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DeleteSelectedUser_WithoutSelection_DoesNothing()
    {
        await viewModel.DeleteSelectedUserCommand.ExecuteAsync(null);

        auth.Verify(x => x.AdminDeleteUser(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task ApplyPasswordResetAndBadgeReset_WithoutSelection_DoNothing()
    {
        await viewModel.ApplyPasswordResetCommand.ExecuteAsync(null);
        await viewModel.ApplyBadgeResetCommand.ExecuteAsync(null);

        auth.Verify(x => x.AdminResetPassword(It.IsAny<User>(), It.IsAny<string>(), default), Times.Never);
        auth.Verify(x => x.AdminResetBadge(It.IsAny<User>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ApplyPasswordReset_Failure_ShowsError()
    {
        auth.Setup(x => x.AdminResetPassword(alice, "x", default)).ReturnsAsync((false, "too short"));
        await viewModel.Load();
        viewModel.SelectedUser = alice;
        viewModel.ResetPassword = "x";

        await viewModel.ApplyPasswordResetCommand.ExecuteAsync(null);

        Assert.Equal("too short", viewModel.StatusMessage);
        Assert.True(viewModel.StatusIsError);
    }

    [Fact]
    public async Task ApplyBadgeReset_Success_ReportsResult()
    {
        auth.Setup(x => x.AdminResetBadge(alice, "badge", default)).ReturnsAsync((true, ""));
        await viewModel.Load();
        viewModel.SelectedUser = alice;
        viewModel.ResetBadge = "badge";

        await viewModel.ApplyBadgeResetCommand.ExecuteAsync(null);

        Assert.Equal("Badge updated for 'alice'.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task Back_InvokesCallback()
    {
        await viewModel.BackCommand.ExecuteAsync(null);

        navigation.Verify(x => x.ShowInventory(), Times.Once);
    }
}
