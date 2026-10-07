namespace BlueHeighliner.Beacon.Data;

/// <summary>
/// Protects the DOD ID, which is displayed and edited in the UI (unlike passwords) so it cannot be a one-way hash.
/// It is stored encrypted for at-rest protection and decrypted for display; <see cref="IDodIdHasher" /> supplies the
/// separate one-way value used for lookups and the uniqueness constraint.
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
}

/// <summary>Encrypts with DPAPI, bound to the current Windows user so no key has to be stored.</summary>
[SupportedOSPlatform("windows")]
internal sealed class DpapiDodIdCipher : IDodIdCipher
{
    private readonly byte[] entropy = Encoding.UTF8.GetBytes("Beacon.DodId.v1");

    public string Encrypt(string plaintext) => string.IsNullOrEmpty(plaintext) ? "" : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), entropy, DataProtectionScope.CurrentUser));

    public string Decrypt(string ciphertext) => string.IsNullOrEmpty(ciphertext) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), entropy, DataProtectionScope.CurrentUser));
}

/// <summary>Encrypts with AES-GCM using a stored key, for platforms without DPAPI.</summary>
internal sealed class AesDodIdCipher(IKeyStore keys) : IDodIdCipher
{
    private readonly byte[] associatedData = Encoding.UTF8.GetBytes("Beacon.DodId.v1");

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return "";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        byte[] cipher = new byte[bytes.Length];
        using AesGcm aes = new(keys.GetOrCreate(), tag.Length);
        aes.Encrypt(nonce, bytes, cipher, tag, associatedData);
        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return "";
        }

        byte[] bytes = Convert.FromBase64String(ciphertext);
        int nonceSize = AesGcm.NonceByteSizes.MaxSize;
        int tagSize = AesGcm.TagByteSizes.MaxSize;
        byte[] plain = new byte[bytes.Length - nonceSize - tagSize];
        using AesGcm aes = new(keys.GetOrCreate(), tagSize);
        aes.Decrypt(bytes.AsSpan(0, nonceSize), bytes.AsSpan(nonceSize + tagSize), bytes.AsSpan(nonceSize, tagSize), plain, associatedData);
        return Encoding.UTF8.GetString(plain);
    }
}
