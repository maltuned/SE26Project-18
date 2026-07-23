using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("recruitments")]
public class Recruitment
{
    public long Id { get; private set; }

    public Game Game { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public int MaxParticipants { get; private set; }

    public int CurrParticipants { get; private set; }

    public RecruitmentStatus Status { get; private set; }

    protected Recruitment() { }

    public DateTime UpdatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public Recruitment(Game game)
    {
        Game = game;
    }

    public void AddParticipant()
    {
        CurrParticipants++;
        UpdatedAt = DateTime.UtcNow;
        if (CurrParticipants >= MaxParticipants)
            Status = RecruitmentStatus.Closed;
    }
}
