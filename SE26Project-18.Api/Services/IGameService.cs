using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IGameService
{
    Task<IReadOnlyCollection<GameResponse>> SearchAsync(
        SearchGamesRequest request,
        CancellationToken ct
    );

    Task<GameResponse?> GetById(long id, CancellationToken ct);

    Task<GameResponse> CreateAsync(CreateGameRequest request, CancellationToken ct);

    Task<GameResponse> UpdateAsync(long id, UpdateGameRequest request, CancellationToken ct);
}
