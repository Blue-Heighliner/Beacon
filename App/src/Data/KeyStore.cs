namespace BlueHeighliner.Beacon.Data;

/// <summary>Provides the symmetric key used to encrypt DOD IDs on platforms without DPAPI.</summary>
internal interface IKeyStore
{
    /// <summary>Gets the key, generating and persisting it on first use.</summary>
    /// <returns>The 32-byte key.</returns>
    byte[] GetOrCreate();
}

internal sealed class FileKeyStore(IAppPaths paths) : IKeyStore
{
    private byte[]? key;

    public byte[] GetOrCreate()
    {
        if (key is not null)
        {
            return key;
        }

        if (File.Exists(paths.KeyPath))
        {
            return key = File.ReadAllBytes(paths.KeyPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(paths.KeyPath)!);
        byte[] created = RandomNumberGenerator.GetBytes(32);
        File.WriteAllBytes(paths.KeyPath, created);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(paths.KeyPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        return key = created;
    }
}
