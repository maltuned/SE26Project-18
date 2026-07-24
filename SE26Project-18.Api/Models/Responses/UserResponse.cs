using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Responses;

public sealed record UserResponse(
    long Id,
    string Username,
    string Nickname,
    string Signature,
    Gender Gender,
    UserStatus Status,
    IReadOnlyCollection<UserTagResponse> Tags
);
