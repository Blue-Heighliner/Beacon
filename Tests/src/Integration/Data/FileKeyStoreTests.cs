namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class FileKeyStoreTests
{
    [Fact]
    public void GetOrCreate_GeneratesThenReusesPersistedKey()
    {
        using TempDirectory directory = new();
        AppPaths paths = new(Path.Combine(directory.Path, "nested"));

        byte[] first = new FileKeyStore(paths).GetOrCreate();
        byte[] second = new FileKeyStore(paths).GetOrCreate();

        Assert.Equal(32, first.Length);
        Assert.Equal(first, second);
        Assert.Equal(first, File.ReadAllBytes(paths.KeyPath));
    }

    [Fact]
    public void GetOrCreate_CachesTheKeyInMemory()
    {
        using TempDirectory directory = new();
        AppPaths paths = new(directory.Path);
        FileKeyStore store = new(paths);
        byte[] first = store.GetOrCreate();

        File.Delete(paths.KeyPath);

        Assert.Same(first, store.GetOrCreate());
    }

    [Fact]
    public void GetOrCreate_RestrictsFilePermissions_OnUnix()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using TempDirectory directory = new();
        AppPaths paths = new(directory.Path);

        new FileKeyStore(paths).GetOrCreate();

        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(paths.KeyPath));
    }
}
