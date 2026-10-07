using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace Artemis.Data;

public static class Database
{
    // Stored in %LOCALAPPDATA% rather than next to the exe: the project folder lives
    // under OneDrive, and cloud sync on a live SQLite file risks lock errors.
    public static string DbPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Artemis", "inventory.db");

    public static SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection($"Data Source={DbPath}");
        connection.Open();
        return connection;
    }

    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        using var connection = GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
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
        command.ExecuteNonQuery();

        MigrateUsersTable(connection);

        using var dropOldIndex = connection.CreateCommand();
        dropOldIndex.CommandText = "DROP INDEX IF EXISTS IX_Users_DodId";
        dropOldIndex.ExecuteNonQuery();

        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText =
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_DodIdHash ON Users(DodIdHash) WHERE DodIdHash <> ''";
        indexCommand.ExecuteNonQuery();
    }

    // A database created by the MVP build predates DodId/IsAdmin/DodIdHash.
    private static void MigrateUsersTable(SqliteConnection connection)
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(Users)";
            using var reader = pragma.ExecuteReader();
            while (reader.Read())
                existing.Add(reader.GetString(1));
        }

        var missing = new Dictionary<string, string>
        {
            ["DodId"] = "ALTER TABLE Users ADD COLUMN DodId TEXT NOT NULL DEFAULT ''",
            ["IsAdmin"] = "ALTER TABLE Users ADD COLUMN IsAdmin INTEGER NOT NULL DEFAULT 0",
            ["DodIdHash"] = "ALTER TABLE Users ADD COLUMN DodIdHash TEXT NOT NULL DEFAULT ''",
        };
        foreach (var (column, sql) in missing)
        {
            if (existing.Contains(column))
                continue;
            using var alter = connection.CreateCommand();
            alter.CommandText = sql;
            alter.ExecuteNonQuery();
        }

        EncryptLegacyPlaintextDodIds(connection);
    }

    // DOD ID used to be stored in plaintext (10 digits) with no hash column. Detect those rows
    // by shape and rewrite them to the encrypted-value + hash form new writes use, so the
    // uniqueness index and lookups keep working after upgrade. Safe to re-run: once migrated,
    // a row's DodId is a DPAPI blob and no longer matches the plaintext shape.
    private static void EncryptLegacyPlaintextDodIds(SqliteConnection connection)
    {
        var legacy = new List<(long Id, string DodId)>();
        using (var select = connection.CreateCommand())
        {
            select.CommandText = "SELECT Id, DodId FROM Users WHERE DodId <> ''";
            using var reader = select.ExecuteReader();
            while (reader.Read())
            {
                var value = reader.GetString(1);
                if (value.Length == 10 && value.All(char.IsAsciiDigit))
                    legacy.Add((reader.GetInt64(0), value));
            }
        }

        foreach (var (id, plaintext) in legacy)
        {
            using var update = connection.CreateCommand();
            update.CommandText = "UPDATE Users SET DodId = $dodId, DodIdHash = $dodIdHash WHERE Id = $id";
            update.Parameters.AddWithValue("$dodId", DodIdCipher.Encrypt(plaintext));
            update.Parameters.AddWithValue("$dodIdHash", DodIdCipher.Hash(plaintext));
            update.Parameters.AddWithValue("$id", id);
            update.ExecuteNonQuery();
        }
    }
}
