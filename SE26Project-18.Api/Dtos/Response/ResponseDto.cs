using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Dtos.Response;

public class ResponseDto
{
    public long Id { get; set; }

    public long RecruitmentId { get; set; }

    public string RecruitmentTitle { get; set; } = string.Empty;

    public long ResponderId { get; set; }

    public string ResponderName { get; set; } = string.Empty;

    public long RecruiterId { get; set; }

    public string RecruiterName { get; set; } = string.Empty;

    public string GreetingMessage { get; set; } = string.Empty;

    public long? ChatId { get; set; }

    public ResponseType Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
