using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("chats")]
public class Chat
{
    public long Id { get; private set; }

    public long? RecruitmentId { get; private set; }

    public Recruitment? Recruitment { get; private set; }

    public long RecruiterId { get; private set; }

    public User Recruiter { get; private set; } = null!;

    public long ResponserId { get; private set; }

    public User Responser { get; private set; } = null!;

    public ICollection<Message> Messages { get; private set; } = [];

    public int NewMsgsCntForRecruiter { get; private set; }

    public int NewMsgsCntForResponser { get; private set; }

    public ChatStatus Status { get; private set; }

    private Chat() { }

    public Chat(long recruiterId, long responserId, long? recruitmentId = null)
    {
        RecruiterId = recruiterId;
        ResponserId = responserId;
        RecruitmentId = recruitmentId;
        Status = ChatStatus.Free;
    }

    public void RefreshRecruitment(long? recruitmentId)
    {
        if (recruitmentId is > 0)
        {
            RecruitmentId = recruitmentId;
        }
    }
}
