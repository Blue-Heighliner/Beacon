using System;

namespace Artemis.Models;

public class InventoryItem
{
    public long Id { get; set; }
    public string SerialNumber { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Category { get; set; } = "";
    public string InsertedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public DateTime CreatedAtLocal => CreatedAt.ToLocalTime();
}
