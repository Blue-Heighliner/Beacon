namespace BlueHeighliner.Beacon.Models;

/// <summary>An account that can sign in to the application.</summary>
internal sealed record User
{
    /// <summary>Gets the database identifier; zero until the user has been stored.</summary>
    public long Id { get; init; }

    /// <summary>Gets the unique username.</summary>
    public required string Username { get; init; }

    /// <summary>Gets the base64 PBKDF2 password hash, or null when the account has no password.</summary>
    public string? PasswordHash { get; init; }

    /// <summary>Gets the base64 salt used for <see cref="PasswordHash" />.</summary>
    public string? PasswordSalt { get; init; }

    /// <summary>Gets the SHA-256 hash of the badge barcode, or null when no badge is registered.</summary>
    public string? BarcodeHash { get; init; }

    /// <summary>Gets the plaintext 10-digit DOD ID.</summary>
    public required string DodId { get; init; }

    /// <summary>Gets a value indicating whether the user has administrator rights.</summary>
    public bool IsAdmin { get; init; }

    /// <summary>Gets the UTC creation time.</summary>
    public required DateTime CreatedAt { get; init; }
}
