namespace BlueHeighliner.Beacon.Tests.Unit.Data;

public sealed class DodIdCipherTests
{
    private readonly DodIdCipher cipher = new("unused");

    [Fact]
    public void Hash_IsDeterministicUppercaseHex()
    {
        string hash = cipher.Hash("1234567890");

        Assert.Equal(cipher.Hash("1234567890"), hash);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash.ToUpperInvariant(), hash);
    }

    [Fact]
    public void Hash_DiffersPerValue() => Assert.NotEqual(cipher.Hash("1234567890"), cipher.Hash("1234567891"));

    [Fact]
    public void Hash_OfEmpty_IsEmpty() => Assert.Equal("", cipher.Hash(""));

    [Fact]
    public void EncryptAndDecrypt_OfEmpty_AreEmpty()
    {
        Assert.Equal("", cipher.Encrypt(""));
        Assert.Equal("", cipher.Decrypt(""));
    }
}
