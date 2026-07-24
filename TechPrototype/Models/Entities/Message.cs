using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("messages")]
public class Message
{
    public long Id { get; private set; }

    public long ChatId { get; set; }

    public long SenderId { get; set; }

    public long ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Chat Chat { get; set; } = null!;

    public User Sender { get; set; } = null!;

    public User Receiver { get; set; } = null!;

    public Message(string content)
    {
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }
}
