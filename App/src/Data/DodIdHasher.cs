namespace BlueHeighliner.Beacon.Data;

/// <summary>Computes the deterministic one-way value used for exact-match DOD ID lookups and the uniqueness constraint, so plaintext never reaches a WHERE clause.</summary>
internal interface IDodIdHasher
{
    /// <summary>Hashes a DOD ID.</summary>
    /// <param name="plaintext">The DOD ID, or empty.</param>
    /// <returns>The uppercase hex SHA-256, or empty when <paramref name="plaintext" /> is empty.</returns>
    string Hash(string plaintext);
}

internal sealed class DodIdHasher : IDodIdHasher
{
    public string Hash(string plaintext) => string.IsNullOrEmpty(plaintext) ? "" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
}
