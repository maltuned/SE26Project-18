using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("reports")]
public class Report
{
    public long Id { get; private set; }

    public long ReporterId { get; set; }

    public User Reporter { get; set; } = null!;

    public ReportTargetType TargetType { get; set; }

    public long TargetId { get; set; }

    public ViolationType ViolationType { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Report()
    {
        CreatedAt = DateTime.UtcNow;
    }
}