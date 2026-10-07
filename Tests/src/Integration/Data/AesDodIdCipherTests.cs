namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class AesDodIdCipherTests
{
    [Fact]
    public void Decrypt_WithNewInstanceOverSameKeyFile_ReadsPreviousCiphertext()
    {
        using TempDirectory directory = new();
        AppPaths paths = new(directory.Path);
        string encrypted = new AesDodIdCipher(new FileKeyStore(paths)).Encrypt("1234567890");

        Assert.Equal("1234567890", new AesDodIdCipher(new FileKeyStore(paths)).Decrypt(encrypted));
    }
}
