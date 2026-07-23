using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("chats")]
public class Chat
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; set; }

    public User Recruiter { get; private init; }

    public User Responser { get; private init; }

    public ICollection<Message> messages { get; set; } = [];

    public int NewMsgsCntForRecruiter { get; set; } = 0;

    public int NewMsgsCntForResponser { get; set; } = 0;

    public ChatStatus Status { get; set; } = ChatStatus.Restricted;

    public Chat(Recruitment recruitment, User recruiter, User responser)
    {
        Recruitment = recruitment;
        Recruiter = recruiter;
        Responser = responser;
    }

    /// <summary>EF Core 无参构造函数</summary>
    private Chat() { }
}
