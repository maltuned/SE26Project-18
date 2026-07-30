namespace SE26Project_18.Api.Models.Responses;

public sealed record GameResponse(
    long Id,
    string Name,
    string Description,
    IReadOnlyCollection<GameTagResponse> Tags,
    string IconUrl,
    string CoverUrl
);
