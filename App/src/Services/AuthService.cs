namespace BlueHeighliner.Beacon.Services;

/// <summary>Registration, sign-in, and account management, with the signed-in user held in memory only.</summary>
internal interface IAuthService
{
    /// <summary>Gets the signed-in user, or null.</summary>
    User? CurrentUser { get; }

    /// <summary>Creates the built-in administrator account on first run.</summary>
    /// <param name="cancellation">Cancels the operation.</param>
    Task EnsureAdminAccount(CancellationToken cancellation = default);

    /// <summary>Registers a new account.</summary>
    /// <param name="username">The desired username.</param>
    /// <param name="password">The password, or empty for a badge-only account.</param>
    /// <param name="barcode">The scanned badge, or empty for a password-only account.</param>
    /// <param name="dodId">The 10-digit DOD ID.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether registration succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> Register(string username, string? password, string? barcode, string dodId, CancellationToken cancellation = default);

    /// <summary>Signs in with a username and password.</summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>True if the credentials were valid.</returns>
    Task<bool> LoginWithPassword(string username, string password, CancellationToken cancellation = default);

    /// <summary>Signs in with a scanned badge.</summary>
    /// <param name="barcode">The scanned barcode.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>True if the badge belongs to a user.</returns>
    Task<bool> LoginWithBarcode(string barcode, CancellationToken cancellation = default);

    /// <summary>Signs out the current user.</summary>
    void Logout();

    /// <summary>Changes the current user's password; the current password is required when one is set.</summary>
    /// <param name="currentPassword">The existing password.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the change succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> ChangePassword(string currentPassword, string newPassword, CancellationToken cancellation = default);

    /// <summary>Registers or replaces the current user's badge.</summary>
    /// <param name="barcode">The scanned barcode.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the change succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> ChangeBadge(string barcode, CancellationToken cancellation = default);

    /// <summary>Admin only: updates another user's username and DOD ID.</summary>
    /// <param name="target">The user to update.</param>
    /// <param name="newUsername">The new username.</param>
    /// <param name="newDodId">The new 10-digit DOD ID.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the update succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> AdminUpdateUser(User target, string newUsername, string newDodId, CancellationToken cancellation = default);

    /// <summary>Admin only: sets another user's password without the old one.</summary>
    /// <param name="target">The user to update.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the reset succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> AdminResetPassword(User target, string newPassword, CancellationToken cancellation = default);

    /// <summary>Admin only: replaces another user's badge.</summary>
    /// <param name="target">The user to update.</param>
    /// <param name="barcode">The scanned barcode.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the reset succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> AdminResetBadge(User target, string barcode, CancellationToken cancellation = default);

    /// <summary>Admin only: deletes a non-admin user other than oneself.</summary>
    /// <param name="target">The user to delete.</param>
    /// <param name="cancellation">Cancels the operation.</param>
    /// <returns>Whether the delete succeeded, and the reason when it did not.</returns>
    Task<(bool Success, string Error)> AdminDeleteUser(User target, CancellationToken cancellation = default);
}

internal sealed class AuthService(IUserRepository users, IInputValidator validator, ICredentialHasher hasher, IClock clock) : IAuthService
{
    private readonly string adminUsername = "admin";
    private readonly string adminDefaultPassword = "TestAdmin123";
    private readonly Regex dodIdPattern = new(@"^\d{10}$", RegexOptions.Compiled);

    public User? CurrentUser { get; private set; }

    public async Task EnsureAdminAccount(CancellationToken cancellation = default)
    {
        if (await users.GetByUsername(adminUsername, cancellation) is not null)
        {
            return;
        }

        await users.Create(WithPassword(new User { Username = adminUsername, DodId = "0000000000", IsAdmin = true, CreatedAt = clock.UtcNow }, adminDefaultPassword), cancellation);
    }

    public async Task<(bool Success, string Error)> Register(string username, string? password, string? barcode, string dodId, CancellationToken cancellation = default)
    {
        username = username.Trim();
        dodId = dodId.Trim();

        if (validator.IsSuspicious(username, password, barcode, dodId))
        {
            return (false, validator.RejectionMessage);
        }

        if (username.Length == 0)
        {
            return (false, "Username is required.");
        }

        if (!dodIdPattern.IsMatch(dodId))
        {
            return (false, "DOD ID must be exactly 10 digits.");
        }

        if (string.IsNullOrEmpty(password) && string.IsNullOrWhiteSpace(barcode))
        {
            return (false, "Provide a password, a badge scan, or both.");
        }

        if (await users.GetByUsername(username, cancellation) is not null)
        {
            return (false, $"Username '{username}' is already taken.");
        }

        if (await users.GetByDodId(dodId, cancellation) is not null)
        {
            return (false, "That DOD ID is already registered to another account.");
        }

        User user = new() { Username = username, DodId = dodId, CreatedAt = clock.UtcNow };

        if (!string.IsNullOrEmpty(password))
        {
            user = WithPassword(user, password);
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            string barcodeHash = hasher.HashBarcode(barcode);
            if (await users.GetByBarcodeHash(barcodeHash, cancellation) is not null)
            {
                return (false, "That badge is already registered to another user.");
            }

            user = user with { BarcodeHash = barcodeHash };
        }

        await users.Create(user, cancellation);
        return (true, "");
    }

    public async Task<bool> LoginWithPassword(string username, string password, CancellationToken cancellation = default)
    {
        if (validator.IsSuspicious(username, password))
        {
            return false;
        }

        User? user = await users.GetByUsername(username.Trim(), cancellation);
        if (user is null || !PasswordMatches(user, password))
        {
            return false;
        }

        CurrentUser = user;
        return true;
    }

    public async Task<bool> LoginWithBarcode(string barcode, CancellationToken cancellation = default)
    {
        if (validator.IsSuspicious(barcode))
        {
            return false;
        }

        User? user = await users.GetByBarcodeHash(hasher.HashBarcode(barcode.Trim()), cancellation);
        if (user is null)
        {
            return false;
        }

        CurrentUser = user;
        return true;
    }

    public void Logout() => CurrentUser = null;

    public async Task<(bool Success, string Error)> ChangePassword(string currentPassword, string newPassword, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { } user)
        {
            return (false, "Not signed in.");
        }

        if (validator.IsSuspicious(currentPassword, newPassword))
        {
            return (false, validator.RejectionMessage);
        }

        if (newPassword.Length < 6)
        {
            return (false, "New password must be at least 6 characters.");
        }

        if (user.PasswordHash is not null && user.PasswordSalt is not null && !PasswordMatches(user, currentPassword))
        {
            return (false, "Current password is incorrect.");
        }

        User updated = WithPassword(user, newPassword);
        await users.Update(updated, cancellation);
        CurrentUser = updated;
        return (true, "");
    }

    public async Task<(bool Success, string Error)> ChangeBadge(string barcode, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { } user)
        {
            return (false, "Not signed in.");
        }

        (bool success, string error, User? updated) = await WithBadge(user, barcode, cancellation);
        if (updated is not null)
        {
            await users.Update(updated, cancellation);
            CurrentUser = updated;
        }

        return (success, error);
    }

    public async Task<(bool Success, string Error)> AdminUpdateUser(User target, string newUsername, string newDodId, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { IsAdmin: true })
        {
            return (false, "Admin access required.");
        }

        newUsername = newUsername.Trim();
        newDodId = newDodId.Trim();

        if (validator.IsSuspicious(newUsername, newDodId))
        {
            return (false, validator.RejectionMessage);
        }

        if (newUsername.Length == 0)
        {
            return (false, "Username is required.");
        }

        if (!dodIdPattern.IsMatch(newDodId))
        {
            return (false, "DOD ID must be exactly 10 digits.");
        }

        User? byName = await users.GetByUsername(newUsername, cancellation);
        if (byName is not null && byName.Id != target.Id)
        {
            return (false, $"Username '{newUsername}' is already taken.");
        }

        User? byDodId = await users.GetByDodId(newDodId, cancellation);
        if (byDodId is not null && byDodId.Id != target.Id)
        {
            return (false, "That DOD ID is already registered to another account.");
        }

        await Save(target with { Username = newUsername, DodId = newDodId }, cancellation);
        return (true, "");
    }

    public async Task<(bool Success, string Error)> AdminResetPassword(User target, string newPassword, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { IsAdmin: true })
        {
            return (false, "Admin access required.");
        }

        if (validator.IsSuspicious(newPassword))
        {
            return (false, validator.RejectionMessage);
        }

        if (newPassword.Length < 6)
        {
            return (false, "New password must be at least 6 characters.");
        }

        await Save(WithPassword(target, newPassword), cancellation);
        return (true, "");
    }

    public async Task<(bool Success, string Error)> AdminResetBadge(User target, string barcode, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { IsAdmin: true })
        {
            return (false, "Admin access required.");
        }

        (bool success, string error, User? updated) = await WithBadge(target, barcode, cancellation);
        if (updated is not null)
        {
            await Save(updated, cancellation);
        }

        return (success, error);
    }

    public async Task<(bool Success, string Error)> AdminDeleteUser(User target, CancellationToken cancellation = default)
    {
        if (CurrentUser is not { IsAdmin: true } admin)
        {
            return (false, "Admin access required.");
        }

        if (target.Id == admin.Id)
        {
            return (false, "You cannot delete your own account.");
        }

        if (target.IsAdmin)
        {
            return (false, "Admin accounts cannot be deleted.");
        }

        await users.Delete(target.Id, cancellation);
        return (true, "");
    }

    private async Task Save(User updated, CancellationToken cancellation)
    {
        await users.Update(updated, cancellation);
        if (CurrentUser?.Id == updated.Id)
        {
            CurrentUser = updated;
        }
    }

    private async Task<(bool Success, string Error, User? Updated)> WithBadge(User user, string barcode, CancellationToken cancellation)
    {
        if (validator.IsSuspicious(barcode))
        {
            return (false, validator.RejectionMessage, null);
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            return (false, "Scan a badge first.", null);
        }

        string barcodeHash = hasher.HashBarcode(barcode.Trim());
        User? existing = await users.GetByBarcodeHash(barcodeHash, cancellation);
        if (existing is not null && existing.Id != user.Id)
        {
            return (false, "That badge is already registered to another user.", null);
        }

        return (true, "", user with { BarcodeHash = barcodeHash });
    }

    private User WithPassword(User user, string password)
    {
        (string hash, string salt) = hasher.HashPassword(password);
        return user with { PasswordSalt = salt, PasswordHash = hash };
    }

    private bool PasswordMatches(User user, string password) => user.PasswordHash is not null && user.PasswordSalt is not null && hasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
}
