using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

public static class UserTagMappings
{
    public static UserTagResponse ToResponse(this UserTag tag)
    {
        return new UserTagResponse { Id = tag.Id, Name = tag.Name };
    }
}
