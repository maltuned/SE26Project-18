using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("admins")]
public class Admin
{
    public long Id { get; private set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; private set; }

    public Admin(string username, string passwordHash)
    {
        Username = username;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }
}
