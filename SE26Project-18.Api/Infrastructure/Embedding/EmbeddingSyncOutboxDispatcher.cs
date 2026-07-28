using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Messaging;

namespace SE26Project_18.Api.Infrastructure.Embedding;

internal sealed class EmbeddingSyncOutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _publisher;
    private readonly EmbeddingSyncOptions _options;
    private readonly ILogger<EmbeddingSyncOutboxDispatcher> _logger;

    public EmbeddingSyncOutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        IEventPublisher publisher,
        IOptions<EmbeddingSyncOptions> options,
        ILogger<EmbeddingSyncOutboxDispatcher> logger
    )
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var published = await DispatchBatchAsync(stoppingToken);
                if (published == 0)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(_options.OutboxPollMilliseconds),
                        stoppingToken
                    );
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Embedding sync outbox dispatch iteration failed");
                await Task.Delay(
                    TimeSpan.FromMilliseconds(_options.OutboxPollMilliseconds),
                    stoppingToken
                );
            }
        }
    }

    private async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DeleteExpiredMessagesAsync(db, ct);
        var now = DateTime.UtcNow;
        var candidateIds = await db
            .EmbeddingSyncOutbox.Where(message =>
                message.PublishedAt == null
                && (message.LeaseExpiresAt == null || message.LeaseExpiresAt <= now)
            )
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .Select(message => message.Id)
            .Take(_options.OutboxPublishBatchSize)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
            return 0;

        var leaseId = Guid.NewGuid();
        var leaseExpiresAt = now.AddSeconds(_options.OutboxLeaseSeconds);
        await db
            .EmbeddingSyncOutbox.Where(message =>
                candidateIds.Contains(message.Id)
                && message.PublishedAt == null
                && (message.LeaseExpiresAt == null || message.LeaseExpiresAt <= now)
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(message => message.LeaseId, leaseId)
                        .SetProperty(message => message.LeaseExpiresAt, leaseExpiresAt),
                ct
            );

        var messages = await db
            .EmbeddingSyncOutbox.Where(message => message.LeaseId == leaseId)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .AsNoTracking()
            .ToListAsync(ct);

        var published = 0;
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeat = RenewLeaseAsync(leaseId, heartbeatCts.Token);
        try
        {
            foreach (var message in messages)
            {
                if (heartbeat.IsFaulted)
                    await heartbeat;
                try
                {
                    await _publisher.PublishAsync(
                        EmbeddingSyncRequested.EventName,
                        message.ToEvent(),
                        ct
                    );
                    var affected = await db
                        .EmbeddingSyncOutbox.Where(candidate =>
                            candidate.Id == message.Id && candidate.LeaseId == leaseId
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(
                                        candidate => candidate.PublishedAt,
                                        DateTime.UtcNow
                                    )
                                    .SetProperty(candidate => candidate.LeaseId, (Guid?)null)
                                    .SetProperty(
                                        candidate => candidate.LeaseExpiresAt,
                                        (DateTime?)null
                                    )
                                    .SetProperty(candidate => candidate.LastError, (string?)null),
                            ct
                        );
                    EnsureLeaseOwned(affected, message.Id);
                    published++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to publish embedding sync outbox message {MessageId}",
                        message.Id
                    );
                    var error =
                        exception.Message.Length <= 2_000
                            ? exception.Message
                            : exception.Message[..2_000];
                    var affected = await db
                        .EmbeddingSyncOutbox.Where(candidate =>
                            candidate.Id == message.Id && candidate.LeaseId == leaseId
                        )
                        .ExecuteUpdateAsync(
                            setters =>
                                setters
                                    .SetProperty(
                                        candidate => candidate.PublishAttempts,
                                        candidate => candidate.PublishAttempts + 1
                                    )
                                    .SetProperty(candidate => candidate.LastError, error)
                                    .SetProperty(candidate => candidate.LeaseId, (Guid?)null)
                                    .SetProperty(
                                        candidate => candidate.LeaseExpiresAt,
                                        DateTime.UtcNow.AddMilliseconds(
                                            _options.OutboxPollMilliseconds
                                        )
                                    ),
                            ct
                        );
                    EnsureLeaseOwned(affected, message.Id);
                }
            }
        }
        finally
        {
            heartbeatCts.Cancel();
            try
            {
                await heartbeat;
            }
            catch (OperationCanceledException) when (heartbeatCts.IsCancellationRequested) { }
        }

        return published;
    }

    private async Task RenewLeaseAsync(Guid leaseId, CancellationToken ct)
    {
        var interval = TimeSpan.FromMilliseconds(
            Math.Max(250, _options.OutboxLeaseSeconds * 1_000d / 3d)
        );
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var affected = await db
                .EmbeddingSyncOutbox.Where(message => message.LeaseId == leaseId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            message => message.LeaseExpiresAt,
                            DateTime.UtcNow.AddSeconds(_options.OutboxLeaseSeconds)
                        ),
                    ct
                );
            if (affected == 0)
                throw new InvalidOperationException(
                    $"Embedding sync outbox lease {leaseId} was lost."
                );
        }
    }

    private async Task DeleteExpiredMessagesAsync(AppDbContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddHours(-_options.OutboxRetentionHours);
        var ids = await db
            .EmbeddingSyncOutbox.Where(message =>
                message.PublishedAt != null && message.PublishedAt < cutoff
            )
            .OrderBy(message => message.PublishedAt)
            .Select(message => message.Id)
            .Take(_options.OutboxPublishBatchSize)
            .ToListAsync(ct);
        if (ids.Count > 0)
            await db
                .EmbeddingSyncOutbox.Where(message => ids.Contains(message.Id))
                .ExecuteDeleteAsync(ct);
    }

    private static void EnsureLeaseOwned(int affectedRows, long messageId)
    {
        if (affectedRows != 1)
            throw new InvalidOperationException(
                $"Embedding sync outbox lease was lost for message {messageId}."
            );
    }
}
