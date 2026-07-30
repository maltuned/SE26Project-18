using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services;

namespace SE26Project_18.Api.Infrastructure.Realtime;

internal sealed class MessageWebSocketHandler : IMessageWebSocketHandler
{
    private const int ReceiveBufferSize = 4 * 1024;

    private const int MaxMessageSize = 16 * 1024;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly IMessageConnectionManager _connectionManager;

    private readonly ILogger<MessageWebSocketHandler> _logger;

    public MessageWebSocketHandler(
        IServiceScopeFactory scopeFactory,
        IMessageConnectionManager connectionManager,
        ILogger<MessageWebSocketHandler> logger
    )
    {
        _scopeFactory = scopeFactory;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task HandleAsync(
        HttpContext context,
        long chatId,
        long userId,
        CancellationToken ct
    )
    {
        await MarkAsReadAsync(chatId, userId, ct);

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        if (!_connectionManager.Add(chatId, userId, socket))
        {
            await _connectionManager.CloseAsync(
                chatId,
                socket,
                WebSocketCloseStatus.PolicyViolation,
                "Account suspended."
            );
            return;
        }

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var request = await ReceiveRequestAsync(chatId, socket, ct);
                if (request is null)
                {
                    break;
                }

                try
                {
                    var message = await SendAsync(chatId, userId, request.Content, ct);
                    await _connectionManager.BroadcastAsync(chatId, message, ct);
                }
                catch (ApiException exception)
                {
                    await _connectionManager.CloseAsync(
                        chatId,
                        socket,
                        WebSocketCloseStatus.PolicyViolation,
                        exception.Message
                    );
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The client disconnected or the request was aborted.
        }
        catch (WebSocketException exception)
        {
            _logger.LogDebug(exception, "WebSocket disconnected from chat {ChatId}", chatId);
        }
        finally
        {
            await _connectionManager.CloseAsync(
                chatId,
                socket,
                WebSocketCloseStatus.NormalClosure,
                string.Empty
            );
        }
    }

    private async Task MarkAsReadAsync(long chatId, long userId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        await messageService.MarkAsReadAsync(chatId, userId, ct);
    }

    private async Task<MessageResponse> SendAsync(
        long chatId,
        long userId,
        string content,
        CancellationToken ct
    )
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var messageService = scope.ServiceProvider.GetRequiredService<IMessageService>();
        return await messageService.SendAsync(chatId, userId, content, ct);
    }

    private async Task<SendMessageRequest?> ReceiveRequestAsync(
        long chatId,
        WebSocket socket,
        CancellationToken ct
    )
    {
        var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferSize);
        try
        {
            using var payload = new MemoryStream();
            ValueWebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer.AsMemory(), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    await _connectionManager.CloseAsync(
                        chatId,
                        socket,
                        WebSocketCloseStatus.InvalidMessageType,
                        "Only text messages are supported."
                    );
                    return null;
                }

                if (payload.Length + result.Count > MaxMessageSize)
                {
                    await _connectionManager.CloseAsync(
                        chatId,
                        socket,
                        WebSocketCloseStatus.MessageTooBig,
                        "Message payload is too large."
                    );
                    return null;
                }

                await payload.WriteAsync(buffer.AsMemory(0, result.Count), ct);
            } while (!result.EndOfMessage);

            try
            {
                var request = JsonSerializer.Deserialize<SendMessageRequest>(
                    payload.GetBuffer().AsSpan(0, checked((int)payload.Length)),
                    JsonSerializerOptions.Web
                );
                var content = request?.Content?.Trim();
                if (
                    string.IsNullOrEmpty(content)
                    || content.Length > SendMessageRequest.MaxContentLength
                )
                {
                    await _connectionManager.CloseAsync(
                        chatId,
                        socket,
                        WebSocketCloseStatus.PolicyViolation,
                        $"Content must contain between 1 and {SendMessageRequest.MaxContentLength} characters."
                    );
                    return null;
                }

                return new SendMessageRequest(content);
            }
            catch (JsonException)
            {
                await _connectionManager.CloseAsync(
                    chatId,
                    socket,
                    WebSocketCloseStatus.InvalidPayloadData,
                    "Message payload must be valid JSON."
                );
                return null;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
