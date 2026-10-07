namespace BlueHeighliner.Beacon.Services;

/// <summary>Hashes and verifies passwords and badge barcodes.</summary>
internal interface ICredentialHasher
{
    /// <summary>Hashes a password with a new random salt.</summary>
    /// <param name="password">The password.</param>
    /// <returns>The base64 hash and the base64 salt it was computed with.</returns>
    (string Hash, string Salt) HashPassword(string password);

    /// <summary>Checks a password against a stored hash in constant time.</summary>
    /// <param name="password">The password to check.</param>
    /// <param name="hash">The stored base64 hash.</param>
    /// <param name="salt">The stored base64 salt.</param>
    /// <returns>True if the password matches.</returns>
    bool VerifyPassword(string password, string hash, string salt);

    /// <summary>Hashes a badge barcode.</summary>
    /// <param name="barcode">The scanned barcode.</param>
    /// <returns>The uppercase hex SHA-256.</returns>
    string HashBarcode(string barcode);
}

internal sealed class CredentialHasher : ICredentialHasher
{
    private readonly int pbkdf2Iterations = 100_000;
    private readonly int saltSize = 16;
    private readonly int hashSize = 32;

    public (string Hash, string Salt) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
        return (Convert.ToBase64String(Derive(password, salt)), Convert.ToBase64String(salt));
    }

    public bool VerifyPassword(string password, string hash, string salt) => CryptographicOperations.FixedTimeEquals(Derive(password, Convert.FromBase64String(salt)), Convert.FromBase64String(hash));

    public string HashBarcode(string barcode) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(barcode)));

    private byte[] Derive(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(password, salt, pbkdf2Iterations, HashAlgorithmName.SHA256, hashSize);
}
