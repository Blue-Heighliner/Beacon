namespace BlueHeighliner.Beacon.Data;

/// <summary>
/// Protects the DOD ID, which is displayed and edited in the UI (unlike passwords) so it cannot be a one-way hash.
/// It is stored encrypted for at-rest protection and decrypted for display, with a separate one-way hash column
/// used for exact-match lookups and the uniqueness constraint so plaintext never has to touch a WHERE clause.
/// </summary>
internal interface IDodIdCipher
{
    /// <summary>Encrypts a DOD ID for storage.</summary>
    /// <param name="plaintext">The DOD ID, or empty.</param>
    /// <returns>The base64 ciphertext, or empty when <paramref name="plaintext" /> is empty.</returns>
    string Encrypt(string plaintext);

    /// <summary>Decrypts a stored DOD ID.</summary>
    /// <param name="ciphertext">A value produced by <see cref="Encrypt" />, or empty.</param>
    /// <returns>The plaintext DOD ID, or empty when <paramref name="ciphertext" /> is empty.</returns>
    string Decrypt(string ciphertext);

    /// <summary>Computes the deterministic lookup hash of a DOD ID.</summary>
    /// <param name="plaintext">The DOD ID, or empty.</param>
    /// <returns>The uppercase hex SHA-256, or empty when <paramref name="plaintext" /> is empty.</returns>
    string Hash(string plaintext);
}

/// <summary>
/// Encrypts with DPAPI on Windows, bound to the current user. DPAPI does not exist elsewhere, so other platforms
/// use AES-GCM with a random key kept in a user-only file next to the database.
/// </summary>
internal sealed class DodIdCipher(string keyPath) : IDodIdCipher
{
    private readonly byte[] entropy = Encoding.UTF8.GetBytes("Beacon.DodId.v1");

    private byte[]? key;

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return "";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
        if (OperatingSystem.IsWindows())
        {
            return Convert.ToBase64String(ProtectedData.Protect(bytes, entropy, DataProtectionScope.CurrentUser));
        }

        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] cipher = new byte[bytes.Length];
        using AesGcm aes = new(GetKey(), tag.Length);
        aes.Encrypt(nonce, bytes, cipher, tag, entropy);
        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return "";
        }

        byte[] bytes = Convert.FromBase64String(ciphertext);
        if (OperatingSystem.IsWindows())
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(bytes, entropy, DataProtectionScope.CurrentUser));
        }

        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;
        byte[] plain = new byte[bytes.Length - nonceSize - tagSize];
        using AesGcm aes = new(GetKey(), tagSize);
        aes.Decrypt(bytes.AsSpan(0, nonceSize), bytes.AsSpan(nonceSize + tagSize), bytes.AsSpan(nonceSize, tagSize), plain, entropy);
        return Encoding.UTF8.GetString(plain);
    }

    public string Hash(string plaintext) => string.IsNullOrEmpty(plaintext) ? "" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));

    private byte[] GetKey()
    {
        if (key is not null)
        {
            return key;
        }

        if (File.Exists(keyPath))
        {
            return key = File.ReadAllBytes(keyPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
        byte[] created = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(keyPath, created);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(keyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return key = created;
    }
}
