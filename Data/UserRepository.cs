using System;
using System.Collections.Generic;
using System.Globalization;
using Artemis.Models;
using Microsoft.Data.Sqlite;

namespace Artemis.Data;

public class UserRepository
{
    public void Create(User user)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash, PasswordSalt, BarcodeHash, DodId, DodIdHash, IsAdmin, CreatedAt)
            VALUES ($username, $passwordHash, $passwordSalt, $barcodeHash, $dodId, $dodIdHash, $isAdmin, $createdAt)
            """;
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$passwordHash", (object?)user.PasswordHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$passwordSalt", (object?)user.PasswordSalt ?? DBNull.Value);
        command.Parameters.AddWithValue("$barcodeHash", (object?)user.BarcodeHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$dodId", DodIdCipher.Encrypt(user.DodId));
        command.Parameters.AddWithValue("$dodIdHash", DodIdCipher.Hash(user.DodId));
        command.Parameters.AddWithValue("$isAdmin", user.IsAdmin ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToString("o", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    public void Update(User user)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET Username = $username, PasswordHash = $passwordHash, PasswordSalt = $passwordSalt,
                BarcodeHash = $barcodeHash, DodId = $dodId, DodIdHash = $dodIdHash, IsAdmin = $isAdmin
            WHERE Id = $id
            """;
        command.Parameters.AddWithValue("$id", user.Id);
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$passwordHash", (object?)user.PasswordHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$passwordSalt", (object?)user.PasswordSalt ?? DBNull.Value);
        command.Parameters.AddWithValue("$barcodeHash", (object?)user.BarcodeHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$dodId", DodIdCipher.Encrypt(user.DodId));
        command.Parameters.AddWithValue("$dodIdHash", DodIdCipher.Hash(user.DodId));
        command.Parameters.AddWithValue("$isAdmin", user.IsAdmin ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Users WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public List<User> GetAll()
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users ORDER BY Username";
        var users = new List<User>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            users.Add(ReadUser(reader));
        return users;
    }

    public User? GetByUsername(string username) => GetBy("Username", username);

    public User? GetByBarcodeHash(string barcodeHash) => GetBy("BarcodeHash", barcodeHash);

    public User? GetByDodId(string dodId) => GetBy("DodIdHash", DodIdCipher.Hash(dodId));

    public int CountAdmins()
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE IsAdmin = 1";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static User? GetBy(string column, string value)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Users WHERE {column} = $value";
        command.Parameters.AddWithValue("$value", value);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    private static User ReadUser(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("Id")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash")),
        PasswordSalt = reader.IsDBNull(reader.GetOrdinal("PasswordSalt")) ? null : reader.GetString(reader.GetOrdinal("PasswordSalt")),
        BarcodeHash = reader.IsDBNull(reader.GetOrdinal("BarcodeHash")) ? null : reader.GetString(reader.GetOrdinal("BarcodeHash")),
        DodId = DodIdCipher.Decrypt(reader.GetString(reader.GetOrdinal("DodId"))),
        IsAdmin = reader.GetInt64(reader.GetOrdinal("IsAdmin")) != 0,
        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
    };
}
