namespace BlueHeighliner.Beacon.Tests.Integration.Data;

/// <summary>A real SQLite database in a scratch folder, with the real cipher and hasher.</summary>
internal sealed class DataFixture : IDisposable
{
    private readonly TempDirectory directory = new();

    public DataFixture()
    {
        Paths = new AppPaths(directory.Path);
        Cipher = new AesDodIdCipher(new FileKeyStore(Paths));
        Hasher = new DodIdHasher();
        Database = new Database(Paths, Cipher, Hasher);
        Database.Initialize().GetAwaiter().GetResult();
    }

    public AppPaths Paths { get; }

    public AesDodIdCipher Cipher { get; }

    public DodIdHasher Hasher { get; }

    public Database Database { get; }

    public void Dispose() => directory.Dispose();
}
