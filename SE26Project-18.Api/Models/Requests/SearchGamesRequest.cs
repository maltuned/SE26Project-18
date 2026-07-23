namespace SE26Project_18.Api.Models.Requests;

public sealed class SearchGamesRequest
{
    public string? Query { get; init; }

    public IReadOnlyCollection<long>? TagIds { get; init; }
}
