using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("chats")]
internal class Chat
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; set; }

    public User User1 { get; private init; }

    public User User2 { get; private init; }

    public ICollection<Message> Messages { get; set; } = [];

    public int NewMsgsCntForUser1 { get; set; } = 0;

    public int NewMsgsCntForUser2 { get; set; } = 0;

    public ChatStatus Status { get; set; } = ChatStatus.Restricted;

    public Chat(Recruitment recruitment, User user1, User user2)
    {
        Recruitment = recruitment;
        User1 = user1;
        User2 = user2;
    }
}
