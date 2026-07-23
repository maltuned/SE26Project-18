using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

public static class GameMappings
{
    public static GameResponse ToResponse(this Game game)
    {
        return new GameResponse(
            game.Id,
            game.Name,
            game.Description,
            game.Tags.Select(t => t.ToResponse()).ToList()
        );
    }
}
