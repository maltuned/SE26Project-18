using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Responses;

public sealed record ResponseResponse(
    long Id,
    long RecruitmentId,
    long ResponderId,
    ResponseType Type
);
