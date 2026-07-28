using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IMessageService
{
    Task<MessageResponse> SendAsync(
        long chatId,
        long senderId,
        long receiverId,
        string content,
        CancellationToken ct
    );

    Task<IReadOnlyList<MessageResponse>> GetByChatAsync(long chatId, long userId, CancellationToken ct);
}
