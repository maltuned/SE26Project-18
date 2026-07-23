using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("recruitments")]
public class Recruitment
{
    public long Id { get; private set; }
    public Game Game { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int CurrParticipants { get; set; } = 0;
    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Open;
    public DateTime ExpiresAt { get; set; }
    public DateTime UpdatedAt { get; private set; }

    protected Recruitment() { }

    public Recruitment(Game game, string title, int maxParticipants, DateTime expiresAt)
    {
        Game = game;
        Title = title;
        MaxParticipants = maxParticipants;
        ExpiresAt = expiresAt;
    }

    public void AddParticipant()
    {
        CurrParticipants++;
        UpdatedAt = DateTime.UtcNow;
        if (CurrParticipants >= MaxParticipants)
            Status = RecruitmentStatus.Closed;
    }
}
