using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SE26Project_18.Api.Infrastructure.Realtime;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Tests.Infrastructure.Realtime;

public sealed class WebSocketMessageConnectionManagerTests
{
    [Fact]
    public async Task Broadcast_SendsCamelCaseMessageToEveryConnectionInChat()
    {
        var manager = new WebSocketMessageConnectionManager(
            NullLogger<WebSocketMessageConnectionManager>.Instance
        );
        using var first = new RecordingWebSocket();
        using var second = new RecordingWebSocket();
        using var otherChat = new RecordingWebSocket();
        manager.Add(1, 10, first);
        manager.Add(1, 11, second);
        manager.Add(2, 10, otherChat);
        var message = new MessageResponse(1, 10, "hello", DateTime.UnixEpoch);

        await manager.BroadcastAsync(1, message, CancellationToken.None);

        var firstPayload = Assert.Single(first.SentMessages);
        Assert.Contains("\"id\":1", firstPayload);
        Assert.Contains("\"senderId\":10", firstPayload);
        Assert.Contains("\"content\":\"hello\"", firstPayload);
        Assert.Single(second.SentMessages);
        Assert.Empty(otherChat.SentMessages);
    }

    [Fact]
    public async Task Remove_StopsSendingToConnection()
    {
        var manager = new WebSocketMessageConnectionManager(
            NullLogger<WebSocketMessageConnectionManager>.Instance
        );
        using var socket = new RecordingWebSocket();
        manager.Add(1, 10, socket);
        manager.Remove(1, socket);

        await manager.BroadcastAsync(
            1,
            new MessageResponse(1, 10, "hello", DateTime.UnixEpoch),
            CancellationToken.None
        );

        Assert.Empty(socket.SentMessages);
    }

    [Fact]
    public async Task ConcurrentBroadcasts_SerializeSendsToEachConnection()
    {
        var manager = new WebSocketMessageConnectionManager(
            NullLogger<WebSocketMessageConnectionManager>.Instance
        );
        using var socket = new RecordingWebSocket(sendDelay: TimeSpan.FromMilliseconds(20));
        manager.Add(1, 10, socket);

        await Task.WhenAll(
            manager.BroadcastAsync(
                1,
                new MessageResponse(1, 10, "first", DateTime.UnixEpoch),
                CancellationToken.None
            ),
            manager.BroadcastAsync(
                1,
                new MessageResponse(2, 11, "second", DateTime.UnixEpoch),
                CancellationToken.None
            )
        );

        Assert.Equal(1, socket.MaximumConcurrentSends);
        Assert.Equal(2, socket.SentMessages.Count);
    }

    [Fact]
    public async Task CloseUser_ClosesOnlyThatUsersConnectionsAcrossChats()
    {
        var manager = new WebSocketMessageConnectionManager(
            NullLogger<WebSocketMessageConnectionManager>.Instance
        );
        using var first = new RecordingWebSocket();
        using var second = new RecordingWebSocket();
        using var otherUser = new RecordingWebSocket();
        manager.Add(1, 10, first);
        manager.Add(2, 10, second);
        manager.Add(1, 11, otherUser);

        await manager.CloseUserAsync(10);

        Assert.Equal(WebSocketState.CloseSent, first.State);
        Assert.Equal(WebSocketState.CloseSent, second.State);
        Assert.Equal(WebSocketState.Open, otherUser.State);
    }

    [Fact]
    public async Task CloseUser_BlocksNewConnectionsUntilUserIsAllowed()
    {
        var manager = new WebSocketMessageConnectionManager(
            NullLogger<WebSocketMessageConnectionManager>.Instance
        );
        using var socket = new RecordingWebSocket();

        await manager.CloseUserAsync(10);

        Assert.False(manager.Add(1, 10, socket));
        manager.AllowUser(10);
        Assert.True(manager.Add(1, 10, socket));
    }

    private sealed class RecordingWebSocket : WebSocket
    {
        private WebSocketState _state = WebSocketState.Open;

        private readonly TimeSpan _sendDelay;

        private int _activeSends;

        public List<string> SentMessages { get; } = [];

        public int MaximumConcurrentSends { get; private set; }

        public RecordingWebSocket(TimeSpan sendDelay = default)
        {
            _sendDelay = sendDelay;
        }

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => _state;

        public override string? SubProtocol => null;

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken
        )
        {
            _state = WebSocketState.Closed;
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken
        )
        {
            _state = WebSocketState.CloseSent;
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken
        )
        {
            throw new NotSupportedException();
        }

        public override async Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken
        )
        {
            var activeSends = Interlocked.Increment(ref _activeSends);
            MaximumConcurrentSends = Math.Max(MaximumConcurrentSends, activeSends);
            try
            {
                if (_sendDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_sendDelay, cancellationToken);
                }

                SentMessages.Add(Encoding.UTF8.GetString(buffer));
            }
            finally
            {
                Interlocked.Decrement(ref _activeSends);
            }
        }
    }
}
