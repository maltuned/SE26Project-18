using System.ComponentModel.DataAnnotations;

namespace SE26Project_18.Api.Models.Requests;

public sealed record CreateRecruitmentRequest(
    [Range(1, long.MaxValue)] long GameId,
    [Required, StringLength(200)] string Title,
    [StringLength(4000)] string Description,
    [Range(1, int.MaxValue)] int MaxParticipants,
    DateTime ExpiresAt,
    IReadOnlyCollection<long> RecruitmentTagIds
);
