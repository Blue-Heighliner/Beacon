namespace BlueHeighliner.Beacon.Tests.Integration.Data;

public sealed class DatabaseTests
{
    [Fact]
    public async Task Initialize_CreatesSchema_AndIsRepeatable()
    {
        using DataFixture fixture = new();

        await fixture.Database.Initialize();

        await using SqliteConnection connection = await fixture.Database.Open();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name IN ('Users', 'Items', 'IX_Users_DodIdHash')";
        Assert.Equal(3L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Initialize_MigratesLegacyDatabase_AndEncryptsPlaintextDodIds()
    {
        using TempDirectory directory = new();
        DodIdCipher cipher = new(directory.File("dodid.key"));
        Database database = new(directory.File("inventory.db"), cipher);
        await using (SqliteConnection legacy = new($"Data Source={database.FilePath};Pooling=False"))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Username TEXT NOT NULL UNIQUE COLLATE NOCASE, PasswordHash TEXT NULL, PasswordSalt TEXT NULL, BarcodeHash TEXT NULL UNIQUE, CreatedAt TEXT NOT NULL);
                INSERT INTO Users (Username, CreatedAt) VALUES ('old', '2020-01-01T00:00:00.0000000Z');
                """;
            await create.ExecuteNonQueryAsync();
            await using SqliteCommand addDod = legacy.CreateCommand();
            addDod.CommandText = "ALTER TABLE Users ADD COLUMN DodId TEXT NOT NULL DEFAULT ''; UPDATE Users SET DodId = '1234567890'";
            await addDod.ExecuteNonQueryAsync();
        }

        await database.Initialize();

        UserRepository users = new(database, cipher);
        User? migrated = await users.GetByDodId("1234567890");
        Assert.Equal("old", migrated?.Username);
        Assert.Equal("1234567890", migrated?.DodId);
    }
}
