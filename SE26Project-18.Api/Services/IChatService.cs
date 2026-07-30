using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IChatService
{
    Task<CursorPagedResponse<ChatResponse>> GetChatsAsync(
        long userId,
        string? before,
        int limit,
        CancellationToken ct
    );

    Task<ChatResponse?> GetChatByUserAsync(
        long currentUserId,
        long otherUserId,
        CancellationToken ct
    );

    Task<ChatResponse?> GetChatByIdAsync(long id, long currentUserId, CancellationToken ct);
}
