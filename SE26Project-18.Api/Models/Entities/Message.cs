using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("messages")]
public class Message
{
    public long Id { get; private set; }

    public User Sender { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAt { get; private set; }

    public Message(User sender)
    {
        Sender = sender;
    }
}
