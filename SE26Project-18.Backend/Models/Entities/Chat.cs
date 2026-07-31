using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("chats")]
public class Chat
{
    public long Id { get; private set; }

    public long RecruitmentId { get; set; }

    public long RecruiterId { get; set; }

    public long ResponserId { get; set; }

    public ChatStatus ChatStatus { get; set; } = ChatStatus.Restricted;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? NewMessageAt { get; set; }

    public Recruitment Recruitment { get; set; } = null!;

    public User Recruiter { get; set; } = null!;

    public User Responser { get; set; } = null!;

    public ICollection<Message> Messages { get; set; } = [];

    public Chat()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}