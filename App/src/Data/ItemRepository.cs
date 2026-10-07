namespace BlueHeighliner.Beacon.Data;

/// <summary>Stores and queries inventory items.</summary>
internal interface IItemRepository
{
    /// <summary>Inserts an item.</summary>
    /// <param name="item">The item to store.</param>
    /// <param name="cancellation">Cancels the insert.</param>
    /// <returns>False if the serial number already exists, otherwise true.</returns>
    Task<bool> TryInsert(InventoryItem item, CancellationToken cancellation = default);

    /// <summary>Deletes an item.</summary>
    /// <param name="id">The item's identifier.</param>
    /// <param name="cancellation">Cancels the delete.</param>
    Task Delete(long id, CancellationToken cancellation = default);

    /// <summary>Queries items with optional filters, newest first. Pass null to skip a filter.</summary>
    /// <param name="category">Only items in this category.</param>
    /// <param name="insertedBy">Only items added by this username.</param>
    /// <param name="fromUtc">Inclusive UTC lower bound on creation time.</param>
    /// <param name="toUtcExclusive">Exclusive UTC upper bound on creation time.</param>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The matching items.</returns>
    Task<List<InventoryItem>> Query(string? category = null, string? insertedBy = null, DateTime? fromUtc = null, DateTime? toUtcExclusive = null, CancellationToken cancellation = default);

    /// <summary>Lists the distinct non-empty categories in use.</summary>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The categories, sorted.</returns>
    Task<List<string>> GetCategories(CancellationToken cancellation = default);

    /// <summary>Lists the distinct usernames that have added items.</summary>
    /// <param name="cancellation">Cancels the query.</param>
    /// <returns>The usernames, sorted.</returns>
    Task<List<string>> GetInsertingUsers(CancellationToken cancellation = default);
}

internal sealed class ItemRepository(IDatabase database) : IItemRepository
{
    private readonly int constraintViolation = 19;

    public async Task<bool> TryInsert(InventoryItem item, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Items (SerialNumber, ItemName, Category, InsertedBy, CreatedAt)
            VALUES ($serialNumber, $itemName, $category, $insertedBy, $createdAt)
            """;
        command.Parameters.AddWithValue("$serialNumber", item.SerialNumber);
        command.Parameters.AddWithValue("$itemName", item.ItemName);
        command.Parameters.AddWithValue("$category", item.Category);
        command.Parameters.AddWithValue("$insertedBy", item.InsertedBy);
        command.Parameters.AddWithValue("$createdAt", item.CreatedAt.ToString("o", CultureInfo.InvariantCulture));

        try
        {
            await command.ExecuteNonQueryAsync(cancellation);
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == constraintViolation)
        {
            return false;
        }
    }

    public async Task Delete(long id, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Items WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellation);
    }

    public async Task<List<InventoryItem>> Query(string? category = null, string? insertedBy = null, DateTime? fromUtc = null, DateTime? toUtcExclusive = null, CancellationToken cancellation = default)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();

        string sql = "SELECT * FROM Items WHERE 1=1";
        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND Category = $category";
            command.Parameters.AddWithValue("$category", category);
        }

        if (!string.IsNullOrEmpty(insertedBy))
        {
            sql += " AND InsertedBy = $insertedBy";
            command.Parameters.AddWithValue("$insertedBy", insertedBy);
        }

        // CreatedAt is stored in ISO-8601 round-trip format, so string comparison is chronological.
        if (fromUtc is not null)
        {
            sql += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", fromUtc.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        if (toUtcExclusive is not null)
        {
            sql += " AND CreatedAt < $to";
            command.Parameters.AddWithValue("$to", toUtcExclusive.Value.ToString("o", CultureInfo.InvariantCulture));
        }

        sql += " ORDER BY CreatedAt DESC";
        command.CommandText = sql;

        List<InventoryItem> items = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            items.Add(new InventoryItem
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                SerialNumber = reader.GetString(reader.GetOrdinal("SerialNumber")),
                ItemName = reader.GetString(reader.GetOrdinal("ItemName")),
                Category = reader.GetString(reader.GetOrdinal("Category")),
                InsertedBy = reader.GetString(reader.GetOrdinal("InsertedBy")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            });
        }

        return items;
    }

    public Task<List<string>> GetCategories(CancellationToken cancellation = default) => GetDistinct("Category", cancellation);

    public Task<List<string>> GetInsertingUsers(CancellationToken cancellation = default) => GetDistinct("InsertedBy", cancellation);

    private async Task<List<string>> GetDistinct(string column, CancellationToken cancellation)
    {
        await using SqliteConnection connection = await database.Open(cancellation);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT DISTINCT {column} FROM Items WHERE {column} <> '' ORDER BY {column}";

        List<string> values = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
