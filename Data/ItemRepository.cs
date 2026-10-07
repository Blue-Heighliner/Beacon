using System;
using System.Collections.Generic;
using System.Globalization;
using Artemis.Models;
using Microsoft.Data.Sqlite;

namespace Artemis.Data;

public class ItemRepository
{
    /// <summary>Inserts the item. Returns false if the serial number already exists.</summary>
    public bool TryInsert(InventoryItem item)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
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
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19) // constraint violation (duplicate serial)
        {
            return false;
        }
    }

    public void Delete(long id)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Items WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Queries items with optional filters. Pass null to skip a filter.
    /// Date bounds are UTC; <paramref name="toUtcExclusive"/> is an exclusive upper bound.
    /// </summary>
    public List<InventoryItem> Query(string? category = null, string? insertedBy = null,
        DateTime? fromUtc = null, DateTime? toUtcExclusive = null)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();

        var sql = "SELECT * FROM Items WHERE 1=1";
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

        var items = new List<InventoryItem>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
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

    public List<string> GetCategories() => GetDistinct("Category");

    public List<string> GetInsertingUsers() => GetDistinct("InsertedBy");

    private static List<string> GetDistinct(string column)
    {
        using var connection = Database.GetConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT DISTINCT {column} FROM Items WHERE {column} <> '' ORDER BY {column}";

        var values = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
            values.Add(reader.GetString(0));
        return values;
    }
}
