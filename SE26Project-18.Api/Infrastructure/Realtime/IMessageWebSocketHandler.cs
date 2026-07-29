namespace SE26Project_18.Api.Infrastructure.Realtime;

public interface IMessageWebSocketHandler
{
    Task HandleAsync(HttpContext context, long chatId, long userId, CancellationToken ct);
}
