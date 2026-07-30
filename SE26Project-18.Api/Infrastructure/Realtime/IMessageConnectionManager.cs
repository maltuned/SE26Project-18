using System.Net.WebSockets;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Infrastructure.Realtime;

public interface IMessageConnectionManager
{
    bool Add(long chatId, long userId, WebSocket socket);

    void Remove(long chatId, WebSocket socket);

    Task BroadcastAsync(long chatId, MessageResponse message, CancellationToken ct);

    Task CloseUserAsync(long userId);

    void AllowUser(long userId);

    Task CloseAsync(
        long chatId,
        WebSocket socket,
        WebSocketCloseStatus closeStatus,
        string description
    );
}
