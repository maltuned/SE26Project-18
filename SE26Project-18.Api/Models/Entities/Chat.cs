using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("chats")]
public class Chat
{
    public long Id { get; private set; }

    public Recruitment Recruitment { get; private set; }

    public User Recruiter { get; private set; }

    public User Responser { get; private set; }

    public ICollection<Message> messages { get; private set; } = [];

    public int NewMsgsCntForRecruiter { get; private set; }

    public int NewMsgsCntForResponser { get; private set; }

    public ChatStatus Status { get; private set; }

    public Chat(Recruitment recruitment, User recruiter, User responser)
    {
        Recruitment = recruitment;
        Recruiter = recruiter;
        Responser = responser;
    }
}
