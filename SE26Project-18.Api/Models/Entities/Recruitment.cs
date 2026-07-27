using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("recruitments")]
internal class Recruitment
{
    public long Id { get; private set; }

    public Game Game { get; private init; }

    public User Recruiter { get; private init; }

    public string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public int CurrParticipants { get; set; } = 0;

    [ConcurrencyCheck]
    public int Version { get; private set; }

    public ICollection<Response> Responses { get; set; } = [];

    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Open;

    public DateTime ExpiresAt { get; set; }

    public Recruitment(
        Game game,
        User recruiter,
        string title,
        int maxParticipants,
        DateTime expiresAt
    )
    {
        Game = game;
        Recruiter = recruiter;
        Title = title;
        MaxParticipants = maxParticipants;
        ExpiresAt = expiresAt;
    }

    public void AddParticipant()
    {
        CurrParticipants++;
        Version++;
        if (CurrParticipants >= MaxParticipants)
            Status = RecruitmentStatus.Closed;
    }
}
