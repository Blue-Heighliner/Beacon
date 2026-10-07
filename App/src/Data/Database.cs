namespace BlueHeighliner.Beacon.Data;

/// <summary>Owns the SQLite database file: opening connections and creating or migrating the schema.</summary>
internal interface IDatabase
{
    /// <summary>Gets the path of the database file.</summary>
    string FilePath { get; }

    /// <summary>Opens a new connection to the database.</summary>
    /// <param name="cancellation">Cancels the open.</param>
    /// <returns>An open connection the caller must dispose.</returns>
    Task<SqliteConnection> Open(CancellationToken cancellation = default);

    /// <summary>Creates the database file and schema if needed, and migrates older schemas in place.</summary>
    /// <param name="cancellation">Cancels initialization.</param>
    Task Initialize(CancellationToken cancellation = default);
}

internal sealed class Database(IAppPaths paths, IDodIdCipher cipher, IDodIdHasher hasher) : IDatabase
{
    private readonly string createSchemaSql = """
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE COLLATE NOCASE,
            PasswordHash TEXT NULL,
            PasswordSalt TEXT NULL,
            BarcodeHash TEXT NULL UNIQUE,
            DodId TEXT NOT NULL DEFAULT '',
            DodIdHash TEXT NOT NULL DEFAULT '',
            IsAdmin INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Items (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SerialNumber TEXT NOT NULL UNIQUE,
            ItemName TEXT NOT NULL DEFAULT '',
            Category TEXT NOT NULL DEFAULT '',
            InsertedBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL
        );
        """;

    private readonly Dictionary<string, string> missingColumnSql = new()
    {
        ["DodId"] = "ALTER TABLE Users ADD COLUMN DodId TEXT NOT NULL DEFAULT ''",
        ["IsAdmin"] = "ALTER TABLE Users ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0",
        ["DodIdHash"] = "ALTER TABLE Users ADD COLUMN DodIdHash TEXT NOT NULL DEFAULT ''",
    };

    public string FilePath => paths.DatabasePath;

    public async Task<SqliteConnection> Open(CancellationToken cancellation = default)
    {
        SqliteConnection connection = new($"Data Source={paths.DatabasePath};Pooling=False");
        await connection.OpenAsync(cancellation);
        return connection;
    }

    public async Task Initialize(CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(paths.DatabasePath)!);

        await using SqliteConnection connection = await Open(cancellation);
        await Execute(connection, createSchemaSql, cancellation);
        await MigrateUsersTable(connection, cancellation);
        await Execute(connection, "DROP INDEX IF EXISTS IX_Users_DodId", cancellation);
        await Execute(connection, "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_DodIdHash ON Users(DodIdHash) WHERE DodIdHash <> ''", cancellation);
    }

    private static async Task Execute(SqliteConnection connection, string sql, CancellationToken cancellation)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellation);
    }

    // A database created by the MVP build predates DodId/IsAdmin/DodIdHash.
    private async Task MigrateUsersTable(SqliteConnection connection, CancellationToken cancellation)
    {
        HashSet<string> existing = new(StringComparer.OrdinalIgnoreCase);
        await using (SqliteCommand pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(Users)";
            await using SqliteDataReader reader = await pragma.ExecuteReaderAsync(cancellation);
            while (await reader.ReadAsync(cancellation))
            {
                existing.Add(reader.GetString(1));
            }
        }

        foreach ((string column, string sql) in missingColumnSql)
        {
            if (!existing.Contains(column))
            {
                await Execute(connection, sql, cancellation);
            }
        }

        await EncryptLegacyPlaintextDodIds(connection, cancellation);
    }

    // DOD ID used to be stored in plaintext (10 digits) with no hash column. Detect those rows
    // by shape and rewrite them to the encrypted-value + hash form new writes use, so the
    // uniqueness index and lookups keep working after upgrade. Safe to re-run: once migrated,
    // a row's DodId is an encrypted blob and no longer matches the plaintext shape.
    private async Task EncryptLegacyPlaintextDodIds(SqliteConnection connection, CancellationToken cancellation)
    {
        List<(long Id, string DodId)> legacy = [];
        await using (SqliteCommand select = connection.CreateCommand())
        {
            select.CommandText = "SELECT Id, DodId FROM Users WHERE DodId <> ''";
            await using SqliteDataReader reader = await select.ExecuteReaderAsync(cancellation);
            while (await reader.ReadAsync(cancellation))
            {
                string value = reader.GetString(1);
                if (value.Length == 10 && value.All(char.IsAsciiDigit))
                {
                    legacy.Add((reader.GetInt64(0), value));
                }
            }
        }

        foreach ((long id, string plaintext) in legacy)
        {
            await using SqliteCommand update = connection.CreateCommand();
            update.CommandText = "UPDATE Users SET DodId = $dodId, DodIdHash = $dodIdHash WHERE Id = $id";
            update.Parameters.AddWithValue("$dodId", cipher.Encrypt(plaintext));
            update.Parameters.AddWithValue("$dodIdHash", hasher.Hash(plaintext));
            update.Parameters.AddWithValue("$id", id);
            await update.ExecuteNonQueryAsync(cancellation);
        }
    }
}
