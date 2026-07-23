using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Entities;

[Table("recruitments")]
public class Recruitment
{
    public long Id { get; private set; }

    public Game Game { get; private init; }

    public string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public int MaxParticipants { get; set; }

    public int CurrParticipants { get; set; } = 0;

    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Open;

    public DateTime ExpiresAt { get; set; }

    public Recruitment(Game game, string title, int maxParticipants, DateTime expiresAt)
    {
        Game = game;
        Title = title;
        MaxParticipants = maxParticipants;
        ExpiresAt = expiresAt;
    }

    /// <summary>EF Core 无参构造函数</summary>
    private Recruitment() { }
}
