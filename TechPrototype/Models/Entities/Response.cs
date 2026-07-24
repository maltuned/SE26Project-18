using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Models.Entities;

[Table("responses")]
public class Response
{
    public long Id { get; private set; }

    public long RecruitmentId { get; set; }

    public long ResponserId { get; set; }

    public ResponseStatus ResponseStatus { get; set; } = ResponseStatus.Responded;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Recruitment Recruitment { get; set; } = null!;

    public User Responser { get; set; } = null!;

    public Response()
    {
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
