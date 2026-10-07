namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IInputValidator> validator = new();
    private readonly Mock<ICredentialHasher> hasher = new();
    private readonly Mock<IClock> clock = new();
    private readonly DateTime now = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
    private readonly AuthService auth;

    public AuthServiceTests()
    {
        validator.Setup(x => x.RejectionMessage).Returns("rejected");
        hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string p) => ("hash:" + p, "salt"));
        hasher.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((string p, string h, string _) => h == "hash:" + p);
        hasher.Setup(x => x.HashBarcode(It.IsAny<string>())).Returns((string b) => "barcode:" + b);
        clock.Setup(x => x.UtcNow).Returns(now);
        auth = new AuthService(users.Object, validator.Object, hasher.Object, clock.Object);
    }

    [Fact]
    public async Task EnsureAdminAccount_CreatesAdmin_WhenMissing()
    {
        users.Setup(x => x.GetByUsername("admin", default)).ReturnsAsync((User?)null);

        await auth.EnsureAdminAccount();

        users.Verify(x => x.Create(It.Is<User>(u => u.Username == "admin" && u.IsAdmin && u.PasswordHash == "hash:TestAdmin123" && u.PasswordSalt == "salt" && u.CreatedAt == now), default), Times.Once);
    }

    [Fact]
    public async Task EnsureAdminAccount_DoesNothing_WhenPresent()
    {
        users.Setup(x => x.GetByUsername("admin", default)).ReturnsAsync(NewUser("admin"));

        await auth.EnsureAdminAccount();

        users.Verify(x => x.Create(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task Register_RejectsSuspiciousInput()
    {
        validator.Setup(x => x.IsSuspicious(It.IsAny<string?[]>())).Returns(true);

        (bool success, string error) = await auth.Register("bob", "pw", null, "1234567890");

        Assert.False(success);
        Assert.Equal("rejected", error);
    }

    [Theory]
    [InlineData("", "pw", null, "1234567890", "Username is required.")]
    [InlineData("bob", "pw", null, "123", "DOD ID must be exactly 10 digits.")]
    [InlineData("bob", "", " ", "1234567890", "Provide a password, a badge scan, or both.")]
    public async Task Register_ValidatesFields(string username, string? password, string? barcode, string dodId, string expected)
    {
        (bool success, string error) = await auth.Register(username, password, barcode, dodId);

        Assert.False(success);
        Assert.Equal(expected, error);
    }

    [Fact]
    public async Task Register_RejectsTakenUsername()
    {
        users.Setup(x => x.GetByUsername("bob", default)).ReturnsAsync(NewUser("bob"));

        (bool success, string error) = await auth.Register("bob", "pw", null, "1234567890");

        Assert.False(success);
        Assert.Equal("Username 'bob' is already taken.", error);
    }

    [Fact]
    public async Task Register_RejectsTakenDodId()
    {
        users.Setup(x => x.GetByDodId("1234567890", default)).ReturnsAsync(NewUser("other"));

        (bool success, string error) = await auth.Register("bob", "pw", null, "1234567890");

        Assert.False(success);
        Assert.Equal("That DOD ID is already registered to another account.", error);
    }

    [Fact]
    public async Task Register_RejectsTakenBadge()
    {
        users.Setup(x => x.GetByBarcodeHash(It.IsAny<string>(), default)).ReturnsAsync(NewUser("other"));

        (bool success, string error) = await auth.Register("bob", null, "badge", "1234567890");

        Assert.False(success);
        Assert.Equal("That badge is already registered to another user.", error);
    }

    [Fact]
    public async Task Register_CreatesUserWithHashedCredentials()
    {
        (bool success, _) = await auth.Register(" bob ", "secret1", "badge", "1234567890");

        Assert.True(success);
        users.Verify(x => x.Create(It.Is<User>(u => u.Username == "bob" && u.DodId == "1234567890" && u.PasswordHash == "hash:secret1" && u.BarcodeHash == "barcode:badge" && u.CreatedAt == now), default), Times.Once);
    }

    [Fact]
    public async Task LoginWithPassword_SignsIn_WithCorrectPassword()
    {
        await RegisterAndCapture("bob", "secret1");

        Assert.True(await auth.LoginWithPassword("bob", "secret1"));
        Assert.Equal("bob", auth.CurrentUser?.Username);
    }

    [Fact]
    public async Task LoginWithPassword_Fails_WithWrongPasswordOrUnknownUser()
    {
        User bob = await RegisterAndCapture("bob", "secret1");
        users.Setup(x => x.GetByUsername("bob", default)).ReturnsAsync(bob);

        Assert.False(await auth.LoginWithPassword("bob", "wrong"));
        Assert.False(await auth.LoginWithPassword("nobody", "secret1"));
        Assert.Null(auth.CurrentUser);
    }

    [Fact]
    public async Task LoginWithPassword_Fails_ForBadgeOnlyAccount()
    {
        users.Setup(x => x.GetByUsername("bob", default)).ReturnsAsync(NewUser("bob") with { BarcodeHash = "HASH" });

        Assert.False(await auth.LoginWithPassword("bob", "anything"));
    }

    [Fact]
    public async Task LoginWithPassword_Fails_ForSuspiciousInput()
    {
        validator.Setup(x => x.IsSuspicious(It.IsAny<string?[]>())).Returns(true);

        Assert.False(await auth.LoginWithPassword("bob", "x"));
    }

    [Fact]
    public async Task LoginWithBarcode_SignsIn_WhenBadgeKnown()
    {
        users.Setup(x => x.GetByBarcodeHash(It.IsAny<string>(), default)).ReturnsAsync(NewUser("bob"));

        Assert.True(await auth.LoginWithBarcode(" badge "));
        Assert.Equal("bob", auth.CurrentUser?.Username);
    }

    [Fact]
    public async Task LoginWithBarcode_Fails_WhenBadgeUnknown() => Assert.False(await auth.LoginWithBarcode("badge"));

    [Fact]
    public async Task Logout_ClearsCurrentUser()
    {
        users.Setup(x => x.GetByBarcodeHash(It.IsAny<string>(), default)).ReturnsAsync(NewUser("bob"));
        await auth.LoginWithBarcode("badge");

        auth.Logout();

        Assert.Null(auth.CurrentUser);
    }

    [Fact]
    public async Task ChangePassword_RequiresSignIn() => Assert.Equal((false, "Not signed in."), await auth.ChangePassword("a", "bbbbbb"));

    [Fact]
    public async Task ChangePassword_UpdatesUser_WhenCurrentPasswordMatches()
    {
        User bob = await SignInWithPassword("bob", "secret1");

        (bool success, _) = await auth.ChangePassword("secret1", "newsecret");

        Assert.True(success);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == bob.Id && u.PasswordHash == "hash:newsecret"), default), Times.Once);
        Assert.Equal("hash:newsecret", auth.CurrentUser?.PasswordHash);
    }

    [Fact]
    public async Task ChangePassword_Fails_WhenCurrentPasswordWrong()
    {
        await SignInWithPassword("bob", "secret1");

        Assert.Equal((false, "Current password is incorrect."), await auth.ChangePassword("wrong", "newsecret"));
    }

    [Fact]
    public async Task ChangePassword_Fails_WhenNewPasswordTooShort()
    {
        await SignInWithPassword("bob", "secret1");

        Assert.Equal((false, "New password must be at least 6 characters."), await auth.ChangePassword("secret1", "short"));
    }

    [Fact]
    public async Task ChangeBadge_UpdatesCurrentUser()
    {
        await SignInWithPassword("bob", "secret1");

        (bool success, _) = await auth.ChangeBadge("badge");

        Assert.True(success);
        Assert.Equal("barcode:badge", auth.CurrentUser?.BarcodeHash);
        users.Verify(x => x.Update(It.Is<User>(u => u.BarcodeHash != null), default), Times.Once);
    }

    [Fact]
    public async Task ChangeBadge_Fails_WhenBadgeBelongsToSomeoneElse()
    {
        User bob = await SignInWithPassword("bob", "secret1");
        users.Setup(x => x.GetByBarcodeHash(It.IsAny<string>(), default)).ReturnsAsync(NewUser("other") with { Id = bob.Id + 1 });

        Assert.Equal((false, "That badge is already registered to another user."), await auth.ChangeBadge("badge"));
    }

    [Fact]
    public async Task ChangeBadge_Fails_WhenBlank()
    {
        await SignInWithPassword("bob", "secret1");

        Assert.Equal((false, "Scan a badge first."), await auth.ChangeBadge("  "));
    }

    [Fact]
    public async Task AdminOperations_RequireAdmin()
    {
        User target = NewUser("target");

        Assert.Equal((false, "Admin access required."), await auth.AdminUpdateUser(target, "x", "1234567890"));
        Assert.Equal((false, "Admin access required."), await auth.AdminResetPassword(target, "newsecret"));
        Assert.Equal((false, "Admin access required."), await auth.AdminResetBadge(target, "badge"));
        Assert.Equal((false, "Admin access required."), await auth.AdminDeleteUser(target));
    }

    [Fact]
    public async Task AdminUpdateUser_UpdatesUsernameAndDodId()
    {
        await SignInAsAdmin();
        User target = NewUser("target") with { Id = 5 };

        (bool success, _) = await auth.AdminUpdateUser(target, " renamed ", "1234567890");

        Assert.True(success);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == 5 && u.Username == "renamed" && u.DodId == "1234567890"), default), Times.Once);
    }

    [Fact]
    public async Task AdminUpdateUser_Fails_WhenUsernameTakenByAnotherUser()
    {
        await SignInAsAdmin();
        users.Setup(x => x.GetByUsername("taken", default)).ReturnsAsync(NewUser("taken") with { Id = 9 });

        Assert.Equal((false, "Username 'taken' is already taken."), await auth.AdminUpdateUser(NewUser("target") with { Id = 5 }, "taken", "1234567890"));
    }

    [Fact]
    public async Task AdminUpdateUser_Fails_WhenDodIdInvalid()
    {
        await SignInAsAdmin();

        Assert.Equal((false, "DOD ID must be exactly 10 digits."), await auth.AdminUpdateUser(NewUser("target"), "target", "abc"));
    }

    [Fact]
    public async Task AdminResetPassword_SetsNewHash()
    {
        await SignInAsAdmin();
        User target = NewUser("target") with { Id = 5 };

        (bool success, _) = await auth.AdminResetPassword(target, "newsecret");

        Assert.True(success);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == 5 && u.PasswordHash == "hash:newsecret"), default), Times.Once);
    }

    [Fact]
    public async Task AdminResetBadge_SetsBadgeHash()
    {
        await SignInAsAdmin();

        (bool success, _) = await auth.AdminResetBadge(NewUser("target") with { Id = 5 }, "badge");

        Assert.True(success);
        users.Verify(x => x.Update(It.Is<User>(u => u.Id == 5 && u.BarcodeHash == "barcode:badge"), default), Times.Once);
    }

    [Fact]
    public async Task AdminDeleteUser_DeletesOrdinaryUser()
    {
        await SignInAsAdmin();

        (bool success, _) = await auth.AdminDeleteUser(NewUser("target") with { Id = 5 });

        Assert.True(success);
        users.Verify(x => x.Delete(5, default), Times.Once);
    }

    [Fact]
    public async Task AdminDeleteUser_RefusesSelfAndAdmins()
    {
        User admin = await SignInAsAdmin();

        Assert.Equal((false, "You cannot delete your own account."), await auth.AdminDeleteUser(admin));
        Assert.Equal((false, "Admin accounts cannot be deleted."), await auth.AdminDeleteUser(NewUser("other") with { Id = 7, IsAdmin = true }));
    }

    private static User NewUser(string username) => new() { Id = 1, Username = username, DodId = "1234567890", CreatedAt = DateTime.UtcNow };

    private async Task<User> RegisterAndCapture(string username, string password)
    {
        User? created = null;
        users.Setup(x => x.Create(It.IsAny<User>(), default)).Callback<User, CancellationToken>((u, _) => created = u).Returns(Task.CompletedTask);
        await auth.Register(username, password, null, "1234567890");
        users.Setup(x => x.GetByUsername(username, default)).ReturnsAsync(created);
        return created!;
    }

    private async Task<User> SignInWithPassword(string username, string password)
    {
        User user = await RegisterAndCapture(username, password);
        await auth.LoginWithPassword(username, password);
        return user;
    }

    private async Task<User> SignInAsAdmin()
    {
        User? created = null;
        users.Setup(x => x.Create(It.IsAny<User>(), default)).Callback<User, CancellationToken>((u, _) => created = u).Returns(Task.CompletedTask);
        await auth.EnsureAdminAccount();
        users.Setup(x => x.GetByUsername("admin", default)).ReturnsAsync(created! with { Id = 1 });
        await auth.LoginWithPassword("admin", "TestAdmin123");
        return auth.CurrentUser!;
    }
}
