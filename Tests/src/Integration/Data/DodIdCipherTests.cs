namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class DodIdCipherTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_WithoutStoringPlaintext()
    {
        using TempDirectory directory = new();
        DodIdCipher cipher = new(directory.File("dodid.key"));

        string encrypted = cipher.Encrypt("1234567890");

        Assert.NotEqual("1234567890", encrypted);
        Assert.DoesNotContain("1234567890", encrypted);
        Assert.Equal("1234567890", cipher.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_WithNewInstance_ReusesPersistedKey()
    {
        using TempDirectory directory = new();
        string encrypted = new DodIdCipher(directory.File("dodid.key")).Encrypt("1234567890");

        Assert.Equal("1234567890", new DodIdCipher(directory.File("dodid.key")).Decrypt(encrypted));
    }
}
