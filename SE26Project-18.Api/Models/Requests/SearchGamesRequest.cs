namespace SE26Project_18.Api.Models.Requests;

public sealed record SearchGamesRequest(
    string? Query = null,
    IReadOnlyCollection<long>? TagIds = null
);
