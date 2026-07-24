using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IResponseService
{
    Task<List<ResponseDto>> GetResponsesByRecruitmentAsync(long recruitmentId);
    Task<List<ResponseDto>> GetResponsesByUserAsync(long userId);
    Task<ResponseDto> CreateResponseAsync(long recruitmentId, long responserId);
    Task<bool> DeleteResponseAsync(long id, string reason);
    Task<ResponseDto?> UpdateResponseStatusAsync(long id, string responseStatus);
}
