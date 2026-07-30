using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("recruitments")]
public class Recruitment
{
    public long Id { get; private set; }

    public long PublisherId { get; set; }

    public long? GameId { get; set; }

    public string GameName { get; set; } = string.Empty;

    public string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Open;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public int MaxParticipants { get; set; }

    public int CurrentParticipants { get; set; }

    public User Publisher { get; set; } = null!;

    public Game? Game { get; set; }

    public ICollection<GameTag> GameTags { get; set; } = [];

    public ICollection<RecruitmentTag> RecruitmentTags { get; set; } = [];

    public ICollection<Response> Responses { get; set; } = [];

    public ICollection<Chat> Chats { get; set; } = [];

    public Recruitment(string title, DateTime expiredAt, int maxParticipants)
    {
        Title = title;
        ExpiredAt = expiredAt;
        MaxParticipants = maxParticipants;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}