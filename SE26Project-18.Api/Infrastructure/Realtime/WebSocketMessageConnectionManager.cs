using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Infrastructure.Realtime;

internal sealed class WebSocketMessageConnectionManager : IMessageConnectionManager
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<
        long,
        ConcurrentDictionary<WebSocket, SemaphoreSlim>
    > _connections = new();

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _broadcastLocks = new();

    private readonly ILogger<WebSocketMessageConnectionManager> _logger;

    public WebSocketMessageConnectionManager(ILogger<WebSocketMessageConnectionManager> logger)
    {
        _logger = logger;
    }

    public void Add(long chatId, WebSocket socket)
    {
        var chatConnections = _connections.GetOrAdd(chatId, _ => new());
        chatConnections.TryAdd(socket, new SemaphoreSlim(1, 1));
    }

    public void Remove(long chatId, WebSocket socket)
    {
        if (!_connections.TryGetValue(chatId, out var chatConnections))
        {
            return;
        }

        chatConnections.TryRemove(socket, out _);
    }

    public async Task BroadcastAsync(long chatId, MessageResponse message, CancellationToken ct)
    {
        if (!_connections.TryGetValue(chatId, out var chatConnections))
        {
            return;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonSerializerOptions.Web);
        var broadcastLock = _broadcastLocks.GetOrAdd(chatId, _ => new SemaphoreSlim(1, 1));
        await broadcastLock.WaitAsync(ct);
        try
        {
            var sends = chatConnections.Select(connection =>
                SendAsync(chatId, connection.Key, connection.Value, payload, ct)
            );
            await Task.WhenAll(sends);
        }
        finally
        {
            broadcastLock.Release();
        }
    }

    public async Task CloseAsync(
        long chatId,
        WebSocket socket,
        WebSocketCloseStatus closeStatus,
        string description
    )
    {
        SemaphoreSlim? sendLock = null;
        if (_connections.TryGetValue(chatId, out var chatConnections))
        {
            chatConnections.TryRemove(socket, out sendLock);
        }

        if (sendLock is not null)
        {
            await sendLock.WaitAsync();
        }

        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(closeStatus, description, CancellationToken.None);
            }
        }
        catch (WebSocketException exception)
        {
            _logger.LogDebug(
                exception,
                "WebSocket disconnected before the close frame was sent for chat {ChatId}",
                chatId
            );
        }
        catch (ObjectDisposedException)
        {
            // The request may have disposed the socket after a concurrent disconnect.
        }
        finally
        {
            sendLock?.Release();
        }
    }

    private async Task SendAsync(
        long chatId,
        WebSocket socket,
        SemaphoreSlim sendLock,
        byte[] payload,
        CancellationToken ct
    )
    {
        await sendLock.WaitAsync(ct);
        try
        {
            if (socket.State != WebSocketState.Open)
            {
                Remove(chatId, socket);
                return;
            }

            using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            sendCts.CancelAfter(SendTimeout);
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, sendCts.Token);
        }
        catch (WebSocketException exception)
        {
            Remove(chatId, socket);
            _logger.LogDebug(
                exception,
                "Failed to broadcast to a WebSocket in chat {ChatId}",
                chatId
            );
        }
        catch (ObjectDisposedException exception)
        {
            Remove(chatId, socket);
            _logger.LogDebug(
                exception,
                "WebSocket in chat {ChatId} was disposed during broadcast",
                chatId
            );
        }
        catch (OperationCanceledException exception) when (!ct.IsCancellationRequested)
        {
            Remove(chatId, socket);
            _logger.LogWarning(
                exception,
                "Timed out broadcasting to a WebSocket in chat {ChatId}",
                chatId
            );
        }
        finally
        {
            sendLock.Release();
        }
    }
}
