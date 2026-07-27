using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IRecruitmentService
{
    Task<PagedResponse<RecruitmentResponse>> SearchAsync(
        RecruitmentQueryRequest request,
        CancellationToken ct
    );

    Task<PagedResponse<RecruitmentResponse>> GetByRecruiterAsync(
        long recruiterId,
        int page,
        int pageSize,
        CancellationToken ct
    );

    Task<RecruitmentResponse> GetByIdAsync(long id, CancellationToken ct);

    Task<RecruitmentResponse> CreateAsync(
        long recruiterId,
        CreateRecruitmentRequest request,
        CancellationToken ct
    );

    Task<RecruitmentResponse> UpdateAsync(
        long recruiterId,
        long recruitmentId,
        UpdateRecruitmentRequest request,
        CancellationToken ct
    );
}
