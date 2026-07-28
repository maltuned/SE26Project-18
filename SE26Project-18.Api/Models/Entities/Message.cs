using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Api.Models.Entities;

[Table("messages")]
internal class Message
{
    public long Id { get; private set; }

    public Chat Chat { get; private init; }

    public User Sender { get; private init; }

    public User Receiver { get; private init; }

    public string Content { get; private init; }

    public DateTime SentAt { get; private init; }

    public Message(Chat chat, User sender, User receiver, string content, DateTime sentAt)
    {
        Chat = chat;
        Sender = sender;
        Receiver = receiver;
        Content = content;
        SentAt = sentAt;
    }

#pragma warning disable CS8618
    private Message() { } // EF Core
#pragma warning restore CS8618
}
