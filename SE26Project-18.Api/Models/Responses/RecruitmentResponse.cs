using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Responses;

public sealed record RecruitmentResponse(
    long Id,
    GameResponse Game,
    UserResponse Recruiter,
    string Title,
    string Description,
    IReadOnlyCollection<RecruitmentTagResponse> Tags,
    int MaxParticipants,
    int CurrParticipants,
    RecruitmentStatus Status,
    DateTime ExpiresAt
);
