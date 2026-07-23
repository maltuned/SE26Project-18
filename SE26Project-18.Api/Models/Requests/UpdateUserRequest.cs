using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Requests;

public sealed class UpdateUserRequest
{
    public string? Nickname { get; init; }

    public string? Signature { get; init; }

    public Gender? Gender { get; init; }

    public IReadOnlyCollection<long>? TagIds { get; init; }
}
