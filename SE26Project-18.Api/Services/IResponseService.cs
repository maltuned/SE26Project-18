using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IResponseService
{
    Task<ResponseResponse> CreateAsync(long userId, long recruitmentId, CancellationToken ct);

    Task<ResponseResponse> GetByIdAsync(long responseId, long userId, CancellationToken ct);

    Task<IReadOnlyList<ResponseResponse>> GetByRecruitmentAsync(
        long recruitmentId,
        long recruiterId,
        CancellationToken ct
    );

    Task<ResponseResponse> AcceptAsync(long responseId, long recruiterId, CancellationToken ct);

    Task<ResponseResponse> RejectAsync(long responseId, long recruiterId, CancellationToken ct);
}
