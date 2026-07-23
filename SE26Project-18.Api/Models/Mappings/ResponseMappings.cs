using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

public static class ResponseMappings
{
    public static ResponseResponse ToResponse(this Response response)
    {
        return new ResponseResponse(
            response.Id,
            response.Recruitment.Id,
            response.Responder.Id,
            response.Type
        );
    }
}
