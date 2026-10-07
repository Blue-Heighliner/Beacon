namespace BlueHeighliner.Beacon.Data;

/// <summary>Stores and queries user accounts.</summary>
internal interface IUserRepository
{
    /// <summary>Inserts a user.</summary>
    /// <param name="user">The user to store.</param>
    /// <param name="cancellation">Cancels the insert.</param>
    Task Create(User user, CancellationToken cancellation = default);

    /// <summary>Overwrites the stored user with the same identifier.</summary>
    /// <param name="user">The user's new state.</param>
    /// <param name="cancellation">Cancels the update.</param>
    Task Update(User user, CancellationToken cancellation = default);

    /// <summary>Deletes a user.</summary>
    /// <param name="id">The user's identifier.</param>
    /// <param name="cancellation">Cancels the delete.</param>
    Task Delete(long id, CancellationToken cancellation = default);

    /// <summary>Lists every user ordered by username.</summary>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>All users.</returns>
    Task<List<User>> GetAll(CancellationToken cancellation = default);

    /// <summary>Finds a user by username, case-insensitively.</summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The user, or null.</returns>
    Task<User?> GetByUsername(string username, CancellationToken cancellation = default);

    /// <summary>Finds a user by badge hash.</summary>
    /// <param name="barcodeHash">The SHA-256 hex hash of the barcode.</param>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The user, or null.</returns>
    Task<User?> GetByBarcodeHash(string barcodeHash, CancellationToken cancellation = default);

    /// <summary>Finds a user by plaintext DOD ID.</summary>
    /// <param name="dodId">The DOD ID.</param>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The user, or null.</returns>
    Task<User?> GetByDodId(string dodId, CancellationToken cancellation = default);

    /// <summary>Counts the administrator accounts.</summary>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The number of administrators.</returns>
    Task<int> CountAdmins(CancellationToken cancellation = default);
}

internal sealed class UserRepository(IDatabase database, IDodIdCipher cipher, IDodIdHasher hasher) : IUserRepository
{
    public async Task Create(User user, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (Username, PasswordHash, PasswordSalt, BarcodeHash, DodId, DodIdHash, IsAdmin, CreatedAt)
            VALUES ($username, $passwordHash, $passwordSalt, $barcodeHash, $dodId, $dodIdHash, $isAdmin, $createdAt)
            """;
        AddUserParameters(command, user);
        command.Parameters.AddWithValue("$createdAt", user.CreatedAt.ToString("o", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task Update(User user, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET Username = $username, PasswordHash = $passwordHash, PasswordSalt = $passwordSalt,
                BarcodeHash = $barcodeHash, DodId = $dodId, DodIdHash = $dodIdHash, IsAdmin = $isAdmin
            WHERE Id = $id
            """;
        command.Parameters.AddWithValue("$id", user.Id);
        AddUserParameters(command, user);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task Delete(long id, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Users WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task<List<User>> GetAll(CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Users ORDER BY Username";
        List<User> users = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            users.Add(ReadUser(reader));
        }

        return users;
    }

    public Task<User?> GetByUsername(string username, CancellationToken cancellation = default) => GetBy("Username", username, cancellation);

    public Task<User?> GetByBarcodeHash(string barcodeHash, CancellationToken cancellation = default) => GetBy("BarcodeHash", barcodeHash, cancellation);

    public Task<User?> GetByDodId(string dodId, CancellationToken cancellation = default) => GetBy("DodIdHash", hasher.Hash(dodId), cancellation);

    public async Task<int> CountAdmins(CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE IsAdmin = 1";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellation));
    }

    private async Task<User?> GetBy(string column, string value, CancellationToken cancellation)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM Users WHERE {column} = $value";
        command.Parameters.AddWithValue("$value", value);
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellation);
        return await reader.ReadAsync(cancellation) ? ReadUser(reader) : null;
    }

    private void AddUserParameters(SqliteCommand command, User user)
    {
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$passwordHash", (object?)user.PasswordHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$passwordSalt", (object?)user.PasswordSalt ?? DBNull.Value);
        command.Parameters.AddWithValue("$barcodeHash", (object?)user.BarcodeHash ?? DBNull.Value);
        command.Parameters.AddWithValue("$dodId", cipher.Encrypt(user.DodId));
        command.Parameters.AddWithValue("$dodIdHash", hasher.Hash(user.DodId));
        command.Parameters.AddWithValue("$isAdmin", user.IsAdmin ? 1 : 0);
    }

    private User ReadUser(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("Id")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? null : reader.GetString(reader.GetOrdinal("PasswordHash")),
        PasswordSalt = reader.IsDBNull(reader.GetOrdinal("PasswordSalt")) ? null : reader.GetString(reader.GetOrdinal("PasswordSalt")),
        BarcodeHash = reader.IsDBNull(reader.GetOrdinal("BarcodeHash")) ? null : reader.GetString(reader.GetOrdinal("BarcodeHash")),
        DodId = cipher.Decrypt(reader.GetString(reader.GetOrdinal("DodId"))),
        IsAdmin = reader.GetInt64(reader.GetOrdinal("IsAdmin")) != 0,
        CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
    };
}
