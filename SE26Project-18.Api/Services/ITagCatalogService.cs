using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface ITagCatalogService
{
    Task<GameTagResponse> CreateGameTagAsync(CreateTagRequest request, CancellationToken ct);
    Task<UserTagResponse> CreateUserTagAsync(CreateTagRequest request, CancellationToken ct);
    Task<RecruitmentTagResponse> CreateRecruitmentTagAsync(
        CreateTagRequest request,
        CancellationToken ct
    );
}
