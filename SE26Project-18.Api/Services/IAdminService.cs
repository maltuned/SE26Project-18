using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IAdminService
{
    Task<PagedResponse<UserResponse>> GetUsersAsync(
        AdminUserQueryRequest request,
        CancellationToken ct
    );

    Task<PagedResponse<GameResponse>> GetGamesAsync(
        AdminGameQueryRequest request,
        CancellationToken ct
    );

    Task<PagedResponse<RecruitmentResponse>> GetRecruitmentsAsync(
        AdminRecruitmentQueryRequest request,
        CancellationToken ct
    );
}
