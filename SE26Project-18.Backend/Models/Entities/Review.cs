using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("reviews")]
public class Review
{
    public long Id { get; private set; }

    public long ReviewerId { get; set; }

    public User Reviewer { get; set; } = null!;

    public long RevieweeId { get; set; }

    public User Reviewee { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public ReviewStatus Status { get; set; } = ReviewStatus.Visible;

    public DateTime CreatedAt { get; set; }

    public Review()
    {
        CreatedAt = DateTime.UtcNow;
    }
}