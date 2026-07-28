using System.ComponentModel.DataAnnotations;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Requests;

public sealed record UpdateRecruitmentRequest(
    [StringLength(200)] string? Title = null,
    [StringLength(4000)] string? Description = null,
    [Range(1, int.MaxValue)] int? MaxParticipants = null,
    DateTime? ExpiresAt = null,
    [EnumDataType(typeof(RecruitmentStatus))] RecruitmentStatus? Status = null,
    IReadOnlyCollection<long>? RecruitmentTagIds = null
);
