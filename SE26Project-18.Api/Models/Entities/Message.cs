using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("messages")]
internal class Message
{
    public long Id { get; private set; }

    public User Sender { get; private init; }

    public string Content { get; private init; }

    public DateTime SentAt { get; private init; }

    public Message(User sender, string content, DateTime sentAt)
    {
        Sender = sender;
        Content = content;
        SentAt = sentAt;
    }

    private Message()
    {
        Sender = null!;
        Content = string.Empty;
    }
}
