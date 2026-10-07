using System;

namespace Artemis.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? BarcodeHash { get; set; }
    public string DodId { get; set; } = "";
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
}
