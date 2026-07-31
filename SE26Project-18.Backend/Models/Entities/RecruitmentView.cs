using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SE26Project_18.Backend.Models.Entities;

[Table("recruitment_views")]
public sealed class RecruitmentView
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public long RecruitmentId { get; private set; }
    public User User { get; private set; } = null!;
    public Recruitment Recruitment { get; private set; } = null!;
    public int ViewCount { get; private set; } = 1;
    public DateTime LastViewedAt { get; private set; } = DateTime.UtcNow;

    [ConcurrencyCheck]
    public int Version { get; private set; }

    public RecruitmentView(User user, Recruitment recruitment)
    {
        User = user;
        Recruitment = recruitment;
        UserId = user.Id;
        RecruitmentId = recruitment.Id;
    }

    private RecruitmentView() { }

    public void RecordView()
    {
        ViewCount++;
        Version++;
        LastViewedAt = DateTime.UtcNow;
    }
}
