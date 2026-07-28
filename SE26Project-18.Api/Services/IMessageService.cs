using System.Net.WebSockets;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public interface IMessageService
{
    Task<List<MessageResponse>> GetHistoryAsync(long chatId, long userId, CancellationToken ct);
    Task<MessageResponse> SaveAndBroadcastAsync(long chatId, long senderId, string content, CancellationToken ct);
    void AddSocket(long chatId, WebSocket socket);
    void RemoveSocket(long chatId, WebSocket socket);
}
