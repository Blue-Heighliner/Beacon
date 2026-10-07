namespace BlueHeighliner.Beacon.Tests.Integration.Data;

/// <summary>A real SQLite database in a scratch folder, with the real cipher.</summary>
internal sealed class DataFixture : IDisposable
{
    private readonly TempDirectory directory = new();

    public DataFixture()
    {
        Cipher = new DodIdCipher(directory.File("dodid.key"));
        Database = new Database(directory.File("inventory.db"), Cipher);
        Database.Initialize().GetAwaiter().GetResult();
    }

    public DodIdCipher Cipher { get; }

    public Database Database { get; }

    public string DirectoryPath => directory.Path;

    public void Dispose() => directory.Dispose();
}
