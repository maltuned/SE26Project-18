using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("notifications")]
public class Notification
{
    public long Id { get; private set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public Notification()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public Notification(long userId, string title, string body)
    {
        UserId = userId;
        Title = title;
        Body = body;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }
}