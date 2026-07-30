namespace SE26Project_18.Api.Infrastructure.Realtime;

public interface IWebSocketTicketStore
{
    WebSocketTicket Issue(long userId, long chatId);

    bool TryConsume(string ticket, long chatId, out long userId);
}

public sealed record WebSocketTicket(string Value, DateTimeOffset ExpiresAt);
