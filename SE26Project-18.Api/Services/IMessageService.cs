using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IMessageService
{
    Task<CursorPagedResponse<MessageResponse>> GetHistoryAsync(
        long chatId,
        long userId,
        string? before,
        int limit,
        CancellationToken ct
    );

    Task MarkAsReadAsync(long chatId, long userId, CancellationToken ct);

    Task<MessageResponse> SendAsync(
        long chatId,
        long senderId,
        string content,
        CancellationToken ct
    );
}
