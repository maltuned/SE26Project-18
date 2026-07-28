using SE26Project_18.Api.Dtos.Recruitment;

namespace SE26Project_18.Api.Services;

public interface IRecruitmentService
{
    Task<List<RecruitmentListResponse>> GetListAsync(
        string? gameName, List<long>? gameTagIds, List<long>? recruitmentTagIds,
        CancellationToken ct);

    Task<RecruitmentDetailResponse> GetByIdAsync(long id, CancellationToken ct);

    Task<RecruitmentDetailResponse> CreateAsync(long userId, CreateRecruitmentRequest req, CancellationToken ct);

    Task<RecruitmentDetailResponse> UpdateAsync(long id, long userId, UpdateRecruitmentRequest req, CancellationToken ct);

    Task DeleteAsync(long id, long userId, CancellationToken ct);

    Task<List<RecruitmentListResponse>> GetByPublisherIdAsync(long userId, CancellationToken ct);

    Task<RecruitmentListResponse?> GetByChatIdAsync(long chatId, CancellationToken ct);

    Task<List<RecruitmentListResponse>> GetByGameIdAsync(long gameId, CancellationToken ct);
}
