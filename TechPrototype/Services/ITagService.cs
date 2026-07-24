using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface ITagService
{
    Task<List<GameTagDto>> GetGameTagsAsync();
    Task<List<RecruitmentTagDto>> GetRecruitmentTagsAsync();
    Task<GameTagDto> CreateGameTagAsync(string name);
    Task<RecruitmentTagDto> CreateRecruitmentTagAsync(string name);
    Task<GameTagDto?> UpdateGameTagAsync(long id, string name);
    Task<RecruitmentTagDto?> UpdateRecruitmentTagAsync(long id, string name);
    Task<bool> DeleteGameTagAsync(long id);
    Task<bool> DeleteRecruitmentTagAsync(long id);
}
