using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Artemis.Data;

/// <summary>
/// DOD ID is displayed/edited in the UI (unlike passwords), so it can't be a one-way hash.
/// It's stored encrypted via DPAPI for at-rest protection, decrypted for display, with a
/// separate one-way hash column (see <see cref="Database"/>) used for exact-match lookups
/// and the uniqueness constraint so plaintext never has to touch a WHERE clause.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DodIdCipher
{
    // Binds ciphertext to this purpose so it can't be swapped with DPAPI blobs from elsewhere.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Artemis.DodId.v1");

    public static string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return "";

        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plaintext), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    public static string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return "";

        var bytes = ProtectedData.Unprotect(Convert.FromBase64String(ciphertext), Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string Hash(string plaintext) =>
        string.IsNullOrEmpty(plaintext) ? "" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
}
