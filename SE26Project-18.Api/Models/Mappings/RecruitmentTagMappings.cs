using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class RecruitmentTagMappings
{
    public static RecruitmentTagResponse ToResponse(this RecruitmentTag tag)
    {
        return new RecruitmentTagResponse(tag.Id, tag.Name);
    }
}
