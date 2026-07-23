using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IChatService
{
    Task<IReadOnlyList<ChatResponse>> GetChatsAsync(long userId);

    Task<ChatResponse?> GetChatByUsersAsync(long user1Id, long user2Id);

    Task<ChatResponse?> GetChatByIdAsync(long id);

    Task<ChatResponse> CreateChatAsync(long recruitmentId, long user1Id, long user2Id);

    Task<bool> UsersExistAsync(long user1Id, long user2Id);

    Task<bool> RecruitmentExistsAsync(long recruitmentId);
}
