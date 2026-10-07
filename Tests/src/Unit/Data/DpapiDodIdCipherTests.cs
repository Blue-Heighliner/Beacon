namespace BlueHeighliner.Beacon.Tests.Unit.Data;

public sealed class DpapiDodIdCipherTests
{
    [Fact]
    public void EncryptAndDecrypt_RoundTrip_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        DpapiDodIdCipher cipher = new();

        string encrypted = cipher.Encrypt("1234567890");

        Assert.NotEqual("1234567890", encrypted);
        Assert.Equal("1234567890", cipher.Decrypt(encrypted));
        Assert.Equal("", cipher.Encrypt(""));
        Assert.Equal("", cipher.Decrypt(""));
    }
}
