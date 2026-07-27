using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class GameTagMappings
{
    public static GameTagResponse ToResponse(this GameTag tag)
    {
        return new GameTagResponse(tag.Id, tag.Name);
    }
}
