using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IChatService
{
    Task<List<ChatBriefDto>> GetChatsByUserAsync(long userId);
    Task<ChatDto?> GetChatByIdAsync(long chatId, long currentUserId);
    Task<List<ChatDto>> GetChatsByRecruitmentAsync(long recruitmentId);
    Task<ChatDto?> GetChatByUsersAsync(long[] userIds);
    Task<ChatDto> CreateChatAsync(long recruitmentId, long user1Id, long user2Id);
    Task<bool> CloseChatAsync(long chatId);
}
