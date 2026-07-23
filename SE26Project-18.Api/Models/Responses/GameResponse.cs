namespace SE26Project_18.Api.Models.Responses;

public sealed class GameResponse
{
    public required long Id { get; init; }

    public required string Description { get; init; }

    public required IReadOnlyCollection<GameTagResponse> Tags { get; init; }
}
