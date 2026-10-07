namespace BlueHeighliner.Beacon.Tests.Unit.Data;

public sealed class AesDodIdCipherTests
{
    private readonly Mock<IKeyStore> keys = new();
    private readonly AesDodIdCipher cipher;

    public AesDodIdCipherTests()
    {
        keys.Setup(x => x.GetOrCreate()).Returns(new byte[32]);
        cipher = new AesDodIdCipher(keys.Object);
    }

    [Fact]
    public void EncryptAndDecrypt_RoundTrip_WithoutExposingPlaintext()
    {
        string encrypted = cipher.Encrypt("1234567890");

        Assert.DoesNotContain("1234567890", encrypted);
        Assert.Equal("1234567890", cipher.Decrypt(encrypted));
    }

    [Fact]
    public void Encrypt_UsesAFreshNonceEachTime() => Assert.NotEqual(cipher.Encrypt("1234567890"), cipher.Encrypt("1234567890"));

    [Fact]
    public void Decrypt_WithDifferentKey_Fails()
    {
        string encrypted = cipher.Encrypt("1234567890");
        keys.Setup(x => x.GetOrCreate()).Returns(Enumerable.Repeat((byte)1, 32).ToArray());

        Assert.ThrowsAny<CryptographicException>(() => cipher.Decrypt(encrypted));
    }

    [Fact]
    public void EncryptAndDecrypt_OfEmpty_AreEmpty_AndNeverTouchTheKey()
    {
        Assert.Equal("", cipher.Encrypt(""));
        Assert.Equal("", cipher.Decrypt(""));
        keys.Verify(x => x.GetOrCreate(), Times.Never);
    }
}
