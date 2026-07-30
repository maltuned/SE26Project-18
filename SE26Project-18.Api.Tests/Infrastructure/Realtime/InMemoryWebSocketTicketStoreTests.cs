using SE26Project_18.Api.Infrastructure.Realtime;

namespace SE26Project_18.Api.Tests.Infrastructure.Realtime;

public sealed class InMemoryWebSocketTicketStoreTests
{
    [Fact]
    public async Task TryConsume_ReturnsBoundUserOnlyOnceAcrossConcurrentAttempts()
    {
        var store = new InMemoryWebSocketTicketStore(TimeProvider.System);
        var ticket = store.Issue(12, 34);

        var attempts = await Task.WhenAll(
            Enumerable
                .Range(0, 20)
                .Select(_ => Task.Run(() =>
                {
                    var consumed = store.TryConsume(ticket.Value, 34, out var userId);
                    return (consumed, userId);
                }))
        );

        var successfulAttempt = Assert.Single(attempts, attempt => attempt.consumed);
        Assert.Equal(12, successfulAttempt.userId);
    }

    [Fact]
    public void TryConsume_RejectsTicketForWrongChat()
    {
        var store = new InMemoryWebSocketTicketStore(TimeProvider.System);
        var ticket = store.Issue(12, 34);

        Assert.False(store.TryConsume(ticket.Value, 35, out _));
        Assert.False(store.TryConsume(ticket.Value, 34, out _));
    }

    [Fact]
    public void TryConsume_RejectsExpiredTicket()
    {
        var timeProvider = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var store = new InMemoryWebSocketTicketStore(timeProvider);
        var ticket = store.Issue(12, 34);
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        Assert.False(store.TryConsume(ticket.Value, 34, out _));
    }

    [Fact]
    public void Issue_ReplacesExistingTicketForSameUserAndChat()
    {
        var store = new InMemoryWebSocketTicketStore(TimeProvider.System);
        var first = store.Issue(12, 34);
        var second = store.Issue(12, 34);

        Assert.False(store.TryConsume(first.Value, 34, out _));
        Assert.True(store.TryConsume(second.Value, 34, out var userId));
        Assert.Equal(12, userId);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
