namespace BlueHeighliner.Beacon.Tests.Unit.Services;

public sealed class CredentialHasherTests
{
    private readonly CredentialHasher hasher = new();

    [Fact]
    public void HashPassword_UsesFreshSaltPerCall()
    {
        (string hash1, string salt1) = hasher.HashPassword("secret");
        (string hash2, string salt2) = hasher.HashPassword("secret");

        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_AcceptsOnlyTheOriginalPassword()
    {
        (string hash, string salt) = hasher.HashPassword("secret");

        Assert.True(hasher.VerifyPassword("secret", hash, salt));
        Assert.False(hasher.VerifyPassword("Secret", hash, salt));
    }

    [Fact]
    public void HashBarcode_IsDeterministicUppercaseHex()
    {
        Assert.Equal(hasher.HashBarcode("badge"), hasher.HashBarcode("badge"));
        Assert.NotEqual(hasher.HashBarcode("badge"), hasher.HashBarcode("badge2"));
        Assert.Equal(64, hasher.HashBarcode("badge").Length);
        Assert.Equal(hasher.HashBarcode("badge").ToUpperInvariant(), hasher.HashBarcode("badge"));
    }
}
