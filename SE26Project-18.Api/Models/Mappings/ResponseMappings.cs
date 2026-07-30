using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class ResponseMappings
{
    public static ResponseResponse ToResponse(this Response response)
    {
        return new ResponseResponse(
            response.Id,
            response.RecruitmentId,
            response.ResponderId,
            response.Type
        );
    }
}
