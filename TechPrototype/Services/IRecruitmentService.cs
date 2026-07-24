using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IRecruitmentService
{
    Task<List<RecruitmentDetailDto>> GetRecruitmentsAsync(string gameName = "", long[] gameTags = null!, long[] recruitmentTags = null!);
    Task<List<RecruitmentDetailDto>> GetRecruitmentsByGameAsync(long gameId);
    Task<RecruitmentDetailDto?> GetRecruitmentByIdAsync(long id);
    Task<List<RecruitmentDetailDto>> GetRecruitmentsByPublisherIdAsync(long publisherId);
    Task<RecruitmentDetailDto?> GetRecruitmentByChatIdAsync(long chatId);
    Task<RecruitmentDetailDto> CreateRecruitmentAsync(RecruitmentDto dto);
    Task<RecruitmentDetailDto?> UpdateRecruitmentAsync(long id, Dictionary<string, object> data);
    Task<bool> DeleteRecruitmentAsync(long id);
}
