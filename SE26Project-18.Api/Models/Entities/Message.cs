using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("messages")]
public class Message
{
    public long Id { get; private set; }

    public long ChatId { get; private set; }

    public Chat Chat { get; private set; } = null!;

    public long SenderId { get; private set; }

    public User Sender { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAt { get; private set; }

    private Message() { }

    public Message(long chatId, long senderId, string content)
    {
        ChatId = chatId;
        SenderId = senderId;
        Content = content;
        SentAt = DateTime.UtcNow;
    }
}
