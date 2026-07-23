using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Responses;

public sealed class UserResponse
{
    public required long Id { get; init; }

    public required string Username { get; init; }

    public required string Nickname { get; init; }

    public required string Signature { get; init; }

    public required Gender Gender { get; init; }

    public required UserStatus Status { get; init; }

    public required IReadOnlyCollection<UserTagResponse> Tags { get; init; }
}
