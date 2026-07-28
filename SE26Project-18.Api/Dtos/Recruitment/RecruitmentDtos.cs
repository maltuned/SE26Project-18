using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Dtos.Recruitment;

public class CreateRecruitmentRequest
{
    [Required] public long GameId { get; set; }
    [Required, MaxLength(100)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    [Range(1, 100)] public int MaxParticipants { get; set; } = 5;
    [Range(1, 720)] public int DurationHours { get; set; } = 24;
    public List<long> TagIds { get; set; } = [];
}

public class UpdateRecruitmentRequest
{
    [MaxLength(100)] public string? Title { get; set; }
    [MaxLength(500)] public string? Description { get; set; }
    public int? MaxParticipants { get; set; }
}

public class RecruitmentListResponse
{
    public long Id { get; set; }
    public long GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxParticipants { get; set; }
    public int CurrParticipants { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }  // from Version indirectly — use updated version
    public long RecruiterId { get; set; }
    public string RecruiterName { get; set; } = string.Empty;
    public List<TagInfo> GameTags { get; set; } = [];
    public List<TagInfo> RecruitmentTags { get; set; } = [];
}

public class RecruitmentDetailResponse : RecruitmentListResponse
{
    public List<RecruiterResponseInfo> Responses { get; set; } = [];
}

public class TagInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RecruiterResponseInfo
{
    public long Id { get; set; }
    public long ResponderId { get; set; }
    public string ResponderName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}
