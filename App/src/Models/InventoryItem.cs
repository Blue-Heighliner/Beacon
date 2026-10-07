namespace BlueHeighliner.Beacon.Models;

/// <summary>A single piece of tracked equipment.</summary>
internal sealed record InventoryItem
{
    /// <summary>Gets the database identifier; zero until the item has been stored.</summary>
    public long Id { get; init; }

    /// <summary>Gets the unique serial number.</summary>
    public required string SerialNumber { get; init; }

    /// <summary>Gets the display name, possibly empty.</summary>
    public required string ItemName { get; init; }

    /// <summary>Gets the category, possibly empty.</summary>
    public required string Category { get; init; }

    /// <summary>Gets the username of the account that added the item.</summary>
    public required string InsertedBy { get; init; }

    /// <summary>Gets the UTC creation time.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Gets the creation time in the local time zone.</summary>
    public DateTime CreatedAtLocal => CreatedAt.ToLocalTime();
}
