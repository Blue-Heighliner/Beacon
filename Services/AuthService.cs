using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Artemis.Data;
using Artemis.Models;

namespace Artemis.Services;

public class AuthService
{
    private const int Pbkdf2Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const string AdminUsername = "admin";
    private const string AdminDefaultPassword = "TestAdmin123";

    private static readonly Regex DodIdPattern = new(@"^\d{10}$", RegexOptions.Compiled);

    private readonly UserRepository _users = new();

    /// <summary>The user currently logged in, or null. Kept in memory only.</summary>
    public User? CurrentUser { get; private set; }

    /// <summary>Creates the built-in Admin account on first run.</summary>
    public void EnsureAdminAccount()
    {
        if (_users.GetByUsername(AdminUsername) is not null)
            return;

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        _users.Create(new User
        {
            Username = AdminUsername,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(HashPassword(AdminDefaultPassword, salt)),
            DodId = "0000000000",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow,
        });
    }

    public (bool Success, string Error) Register(string username, string? password, string? barcode, string dodId)
    {
        username = username.Trim();
        dodId = dodId.Trim();

        if (InputValidator.IsSuspicious(username, password, barcode, dodId))
            return (false, InputValidator.RejectionMessage);
        if (username.Length == 0)
            return (false, "Username is required.");
        if (!DodIdPattern.IsMatch(dodId))
            return (false, "DOD ID must be exactly 10 digits.");
        if (string.IsNullOrEmpty(password) && string.IsNullOrWhiteSpace(barcode))
            return (false, "Provide a password, a badge scan, or both.");
        if (_users.GetByUsername(username) is not null)
            return (false, $"Username '{username}' is already taken.");
        if (_users.GetByDodId(dodId) is not null)
            return (false, "That DOD ID is already registered to another account.");

        var user = new User { Username = username, DodId = dodId, CreatedAt = DateTime.UtcNow };

        if (!string.IsNullOrEmpty(password))
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            user.PasswordSalt = Convert.ToBase64String(salt);
            user.PasswordHash = Convert.ToBase64String(HashPassword(password, salt));
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var barcodeHash = HashBarcode(barcode);
            if (_users.GetByBarcodeHash(barcodeHash) is not null)
                return (false, "That badge is already registered to another user.");
            user.BarcodeHash = barcodeHash;
        }

        _users.Create(user);
        return (true, "");
    }

    public bool LoginWithPassword(string username, string password)
    {
        if (InputValidator.IsSuspicious(username, password))
            return false;

        var user = _users.GetByUsername(username.Trim());
        if (user?.PasswordHash is null || user.PasswordSalt is null)
            return false;

        var hash = HashPassword(password, Convert.FromBase64String(user.PasswordSalt));
        if (!CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(user.PasswordHash)))
            return false;

        CurrentUser = user;
        return true;
    }

    public bool LoginWithBarcode(string barcode)
    {
        if (InputValidator.IsSuspicious(barcode))
            return false;

        var user = _users.GetByBarcodeHash(HashBarcode(barcode.Trim()));
        if (user is null)
            return false;

        CurrentUser = user;
        return true;
    }

    public void Logout() => CurrentUser = null;

    /// <summary>Self-service password change; requires the current password when one is set.</summary>
    public (bool Success, string Error) ChangePassword(string currentPassword, string newPassword)
    {
        if (CurrentUser is not { } user)
            return (false, "Not signed in.");
        if (InputValidator.IsSuspicious(currentPassword, newPassword))
            return (false, InputValidator.RejectionMessage);
        if (newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        if (user.PasswordHash is not null && user.PasswordSalt is not null)
        {
            var current = HashPassword(currentPassword, Convert.FromBase64String(user.PasswordSalt));
            if (!CryptographicOperations.FixedTimeEquals(current, Convert.FromBase64String(user.PasswordHash)))
                return (false, "Current password is incorrect.");
        }

        SetPassword(user, newPassword);
        _users.Update(user);
        return (true, "");
    }

    /// <summary>Self-service badge (re-)registration.</summary>
    public (bool Success, string Error) ChangeBadge(string barcode)
    {
        if (CurrentUser is not { } user)
            return (false, "Not signed in.");
        if (InputValidator.IsSuspicious(barcode))
            return (false, InputValidator.RejectionMessage);
        if (string.IsNullOrWhiteSpace(barcode))
            return (false, "Scan a badge first.");

        var barcodeHash = HashBarcode(barcode.Trim());
        var existing = _users.GetByBarcodeHash(barcodeHash);
        if (existing is not null && existing.Id != user.Id)
            return (false, "That badge is already registered to another user.");

        user.BarcodeHash = barcodeHash;
        _users.Update(user);
        return (true, "");
    }

    /// <summary>Admin: update another user's username and DOD ID.</summary>
    public (bool Success, string Error) AdminUpdateUser(User target, string newUsername, string newDodId)
    {
        if (CurrentUser is not { IsAdmin: true })
            return (false, "Admin access required.");

        newUsername = newUsername.Trim();
        newDodId = newDodId.Trim();

        if (InputValidator.IsSuspicious(newUsername, newDodId))
            return (false, InputValidator.RejectionMessage);
        if (newUsername.Length == 0)
            return (false, "Username is required.");
        if (!DodIdPattern.IsMatch(newDodId))
            return (false, "DOD ID must be exactly 10 digits.");

        var byName = _users.GetByUsername(newUsername);
        if (byName is not null && byName.Id != target.Id)
            return (false, $"Username '{newUsername}' is already taken.");
        var byDodId = _users.GetByDodId(newDodId);
        if (byDodId is not null && byDodId.Id != target.Id)
            return (false, "That DOD ID is already registered to another account.");

        target.Username = newUsername;
        target.DodId = newDodId;
        _users.Update(target);
        return (true, "");
    }

    /// <summary>Admin: set a new password for another user without knowing the old one.</summary>
    public (bool Success, string Error) AdminResetPassword(User target, string newPassword)
    {
        if (CurrentUser is not { IsAdmin: true })
            return (false, "Admin access required.");
        if (InputValidator.IsSuspicious(newPassword))
            return (false, InputValidator.RejectionMessage);
        if (newPassword.Length < 6)
            return (false, "New password must be at least 6 characters.");

        SetPassword(target, newPassword);
        _users.Update(target);
        return (true, "");
    }

    /// <summary>Admin: replace another user's badge.</summary>
    public (bool Success, string Error) AdminResetBadge(User target, string barcode)
    {
        if (CurrentUser is not { IsAdmin: true })
            return (false, "Admin access required.");
        if (InputValidator.IsSuspicious(barcode))
            return (false, InputValidator.RejectionMessage);
        if (string.IsNullOrWhiteSpace(barcode))
            return (false, "Scan a badge first.");

        var barcodeHash = HashBarcode(barcode.Trim());
        var existing = _users.GetByBarcodeHash(barcodeHash);
        if (existing is not null && existing.Id != target.Id)
            return (false, "That badge is already registered to another user.");

        target.BarcodeHash = barcodeHash;
        _users.Update(target);
        return (true, "");
    }

    public (bool Success, string Error) AdminDeleteUser(User target)
    {
        if (CurrentUser is not { IsAdmin: true } admin)
            return (false, "Admin access required.");
        if (target.Id == admin.Id)
            return (false, "You cannot delete your own account.");
        if (target.IsAdmin)
            return (false, "Admin accounts cannot be deleted.");

        _users.Delete(target.Id);
        return (true, "");
    }

    private static void SetPassword(User user, string newPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        user.PasswordSalt = Convert.ToBase64String(salt);
        user.PasswordHash = Convert.ToBase64String(HashPassword(newPassword, salt));
    }

    private static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSize);

    private static string HashBarcode(string barcode) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(barcode)));
}
