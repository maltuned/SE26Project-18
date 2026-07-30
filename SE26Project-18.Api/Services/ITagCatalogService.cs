using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface ITagCatalogService
{
    Task<IReadOnlyCollection<GameTagResponse>> GetGameTagsAsync(CancellationToken ct);

    Task<GameTagResponse> CreateGameTagAsync(CreateTagRequest request, CancellationToken ct);

    Task<UserTagResponse> CreateUserTagAsync(CreateTagRequest request, CancellationToken ct);

    Task<IReadOnlyCollection<UserTagResponse>> GetUserTagsAsync(CancellationToken ct);

    Task<IReadOnlyCollection<RecruitmentTagResponse>> GetRecruitmentTagsAsync(
        CancellationToken ct
    );

    Task<RecruitmentTagResponse> CreateRecruitmentTagAsync(
        CreateTagRequest request,
        CancellationToken ct
    );
}
