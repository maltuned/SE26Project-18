using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("feedbacks")]
public class Feedback
{
    public long Id { get; private set; }

    public long UserId { get; set; }

    public User User { get; set; } = null!;

    public FeedbackType Type { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Feedback()
    {
        CreatedAt = DateTime.UtcNow;
    }
}