using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Models.Requests;

public sealed record UpdateUserRequest(
    string? Nickname = null,
    string? Signature = null,
    Gender? Gender = null,
    IReadOnlyCollection<long>? TagIds = null
);
