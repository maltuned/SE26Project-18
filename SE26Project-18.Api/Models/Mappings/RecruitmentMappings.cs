using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class RecruitmentMappings
{
    public static RecruitmentResponse ToResponse(this Recruitment recruitment)
    {
        return new RecruitmentResponse(
            recruitment.Id,
            recruitment.Game.ToResponse(),
            recruitment.Recruiter.ToResponse(),
            recruitment.Title,
            recruitment.Description,
            recruitment.Tags.Select(t => t.ToResponse()).ToList(),
            recruitment.MaxParticipants,
            recruitment.CurrParticipants,
            recruitment.Status,
            recruitment.ExpiresAt
        );
    }
}
