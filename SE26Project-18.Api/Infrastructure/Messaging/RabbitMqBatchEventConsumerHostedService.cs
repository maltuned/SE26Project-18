using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal sealed class RabbitMqBatchEventConsumerHostedService<TEvent, TConsumer> : BackgroundService
    where TConsumer : class, IBatchEventConsumer<TEvent>
{
    private readonly ConnectionFactory _connectionFactory;

    private readonly RabbitMqOptions _rabbitOptions;

    private readonly EmbeddingSyncOptions _batchOptions;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly RabbitMqBatchConsumerRegistration<TEvent, TConsumer> _registration;

    private readonly ILogger<RabbitMqBatchEventConsumerHostedService<TEvent, TConsumer>> _logger;

    public RabbitMqBatchEventConsumerHostedService(
        IOptions<RabbitMqOptions> rabbitOptions,
        IOptions<EmbeddingSyncOptions> batchOptions,
        IServiceScopeFactory scopeFactory,
        RabbitMqBatchConsumerRegistration<TEvent, TConsumer> registration,
        ILogger<RabbitMqBatchEventConsumerHostedService<TEvent, TConsumer>> logger
    )
    {
        _rabbitOptions = rabbitOptions.Value;
        _batchOptions = batchOptions.Value;
        _scopeFactory = scopeFactory;
        _registration = registration;
        _logger = logger;
        _connectionFactory = RabbitMqConnectionFactory.Create(
            _rabbitOptions,
            $"se26project-18:{typeof(TConsumer).Name}",
            automaticRecoveryEnabled: true
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to run RabbitMQ batch consumer {ConsumerName}; retrying",
                    typeof(TConsumer).Name
                );
                await Task.Delay(
                    TimeSpan.FromSeconds(_rabbitOptions.RecoveryDelaySeconds),
                    stoppingToken
                );
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var eventName = _registration.EventName;
        var queueName = _registration.QueueName;
        var retryExchangeName = $"{_rabbitOptions.ExchangeName}.retry";
        var retryQueueName = $"{queueName}.retry";
        var deadLetterQueueName = $"{queueName}.dead-letter";

        await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            stoppingToken
        );
        await DeclareTopologyAsync(
            channel,
            eventName,
            queueName,
            retryExchangeName,
            retryQueueName,
            deadLetterQueueName,
            stoppingToken
        );
        await channel.BasicQosAsync(
            0,
            (ushort)_batchOptions.PrefetchCount,
            global: false,
            stoppingToken
        );

        var deliveries = Channel.CreateBounded<PendingDelivery>(
            new BoundedChannelOptions(_batchOptions.PrefetchCount)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait,
            }
        );
        using var channelGate = new SemaphoreSlim(1, 1);
        var batchWorker = ProcessBatchesAsync(
            deliveries.Reader,
            channel,
            channelGate,
            retryExchangeName,
            queueName,
            eventName,
            stoppingToken
        );

        var rabbitConsumer = new AsyncEventingBasicConsumer(channel);
        rabbitConsumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<TEvent>(
                    eventArgs.Body.Span,
                    EventJsonSerializer.Options
                );
                if (payload is null)
                {
                    throw new JsonException("Event payload cannot be null.");
                }

                await deliveries.Writer.WriteAsync(
                    new PendingDelivery(
                        payload,
                        eventArgs.DeliveryTag,
                        eventArgs.Body.ToArray(),
                        eventArgs.BasicProperties
                    ),
                    stoppingToken
                );
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                _logger.LogError(exception, "Invalid batch event {EventName}", eventName);
                await WithChannelLockAsync(
                    channelGate,
                    stoppingToken,
                    () =>
                        channel
                            .BasicNackAsync(eventArgs.DeliveryTag, false, false, stoppingToken)
                            .AsTask()
                );
            }
        };

        await channel.BasicConsumeAsync(queueName, false, rabbitConsumer, stoppingToken);

        try
        {
            var lifetime = Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            var completed = await Task.WhenAny(lifetime, batchWorker);
            if (completed == batchWorker)
            {
                await batchWorker;
            }
            await lifetime;
        }
        finally
        {
            deliveries.Writer.TryComplete();
            if (!batchWorker.IsCompleted)
            {
                try
                {
                    await batchWorker;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Closing the channel requeues all unacknowledged deliveries.
                }
            }
        }
    }

    private async Task ProcessBatchesAsync(
        ChannelReader<PendingDelivery> reader,
        IChannel channel,
        SemaphoreSlim channelGate,
        string retryExchangeName,
        string queueName,
        string eventName,
        CancellationToken stoppingToken
    )
    {
        while (await reader.WaitToReadAsync(stoppingToken))
        {
            var batch = new List<PendingDelivery>(_batchOptions.BatchSize)
            {
                await reader.ReadAsync(stoppingToken),
            };
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            waitCts.CancelAfter(TimeSpan.FromMilliseconds(_batchOptions.MaxBatchWaitMilliseconds));

            while (batch.Count < _batchOptions.BatchSize)
            {
                try
                {
                    batch.Add(await reader.ReadAsync(waitCts.Token));
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            await ProcessBatchAsync(
                batch,
                channel,
                channelGate,
                retryExchangeName,
                queueName,
                eventName,
                stoppingToken
            );
        }
    }

    private async Task ProcessBatchAsync(
        IReadOnlyCollection<PendingDelivery> batch,
        IChannel channel,
        SemaphoreSlim channelGate,
        string retryExchangeName,
        string queueName,
        string eventName,
        CancellationToken stoppingToken
    )
    {
        Exception? processingFailure = null;
        try
        {
            using var processingCts = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken
            );
            processingCts.CancelAfter(TimeSpan.FromSeconds(_batchOptions.ProcessingTimeoutSeconds));
            await using var scope = _scopeFactory.CreateAsyncScope();
            var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
            await consumer.ConsumeAsync(
                batch.Select(item => item.Payload).ToList(),
                processingCts.Token
            );
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            processingFailure = exception;
        }

        if (processingFailure is not null)
        {
            _logger.LogError(
                processingFailure,
                "Failed to process batch of {BatchSize} {EventName} events",
                batch.Count,
                eventName
            );
            if (processingFailure is EmbeddingSyncValidationException && batch.Count > 1)
            {
                var items = batch.ToArray();
                var midpoint = items.Length / 2;
                await ProcessBatchAsync(
                    items[..midpoint],
                    channel,
                    channelGate,
                    retryExchangeName,
                    queueName,
                    eventName,
                    stoppingToken
                );
                await ProcessBatchAsync(
                    items[midpoint..],
                    channel,
                    channelGate,
                    retryExchangeName,
                    queueName,
                    eventName,
                    stoppingToken
                );
                return;
            }

            if (processingFailure is EmbeddingSyncValidationException)
            {
                var delivery = batch.Single();
                await WithChannelLockAsync(
                    channelGate,
                    stoppingToken,
                    () =>
                        channel
                            .BasicNackAsync(delivery.DeliveryTag, false, false, stoppingToken)
                            .AsTask()
                );
                return;
            }

            foreach (var delivery in batch)
            {
                await RetryOrDeadLetterAsync(
                    channel,
                    channelGate,
                    delivery,
                    retryExchangeName,
                    queueName,
                    stoppingToken
                );
            }
            return;
        }

        foreach (var delivery in batch)
        {
            await WithChannelLockAsync(
                channelGate,
                stoppingToken,
                () => channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken).AsTask()
            );
        }
    }

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        SemaphoreSlim channelGate,
        PendingDelivery delivery,
        string retryExchangeName,
        string queueName,
        CancellationToken ct
    )
    {
        var retryCount = GetRetryCount(delivery.Properties.Headers);
        if (retryCount >= _rabbitOptions.MaxRetryAttempts)
        {
            await WithChannelLockAsync(
                channelGate,
                ct,
                () => channel.BasicNackAsync(delivery.DeliveryTag, false, false, ct).AsTask()
            );
            return;
        }

        var headers = delivery.Properties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.Properties.Headers);
        headers["x-retry-count"] = retryCount + 1;
        var properties = new BasicProperties
        {
            ContentType = delivery.Properties.ContentType,
            ContentEncoding = delivery.Properties.ContentEncoding,
            DeliveryMode = delivery.Properties.DeliveryMode,
            MessageId = delivery.Properties.MessageId,
            CorrelationId = delivery.Properties.CorrelationId,
            Timestamp = delivery.Properties.Timestamp,
            Headers = headers,
        };

        await WithChannelLockAsync(
            channelGate,
            ct,
            async () =>
            {
                await channel.BasicPublishAsync(
                    retryExchangeName,
                    queueName,
                    true,
                    properties,
                    delivery.Body,
                    ct
                );
                await channel.BasicAckAsync(delivery.DeliveryTag, false, ct);
            }
        );
    }

    private async Task DeclareTopologyAsync(
        IChannel channel,
        string eventName,
        string queueName,
        string retryExchangeName,
        string retryQueueName,
        string deadLetterQueueName,
        CancellationToken ct
    )
    {
        await channel.ExchangeDeclareAsync(
            _rabbitOptions.ExchangeName,
            ExchangeType.Topic,
            true,
            false,
            cancellationToken: ct
        );
        await channel.ExchangeDeclareAsync(
            _rabbitOptions.DeadLetterExchangeName,
            ExchangeType.Topic,
            true,
            false,
            cancellationToken: ct
        );
        await channel.ExchangeDeclareAsync(
            retryExchangeName,
            ExchangeType.Direct,
            true,
            false,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            deadLetterQueueName,
            true,
            false,
            false,
            new Dictionary<string, object?>
            {
                ["x-max-length"] = _rabbitOptions.DeadLetterQueueMaxLength,
            },
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            deadLetterQueueName,
            _rabbitOptions.DeadLetterExchangeName,
            deadLetterQueueName,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            retryQueueName,
            true,
            false,
            false,
            new Dictionary<string, object?>
            {
                ["x-message-ttl"] = _rabbitOptions.RetryDelaySeconds * 1_000,
                ["x-dead-letter-exchange"] = _rabbitOptions.ExchangeName,
                ["x-dead-letter-routing-key"] = eventName,
            },
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            retryQueueName,
            retryExchangeName,
            queueName,
            cancellationToken: ct
        );
        await channel.QueueDeclareAsync(
            queueName,
            true,
            false,
            false,
            new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _rabbitOptions.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = deadLetterQueueName,
                ["x-single-active-consumer"] = true,
            },
            cancellationToken: ct
        );
        await channel.QueueBindAsync(
            queueName,
            _rabbitOptions.ExchangeName,
            eventName,
            cancellationToken: ct
        );
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value))
        {
            return 0;
        }

        return value switch
        {
            byte count => count,
            short count => count,
            int count => count,
            long count when count <= int.MaxValue => (int)count,
            _ => 0,
        };
    }

    private static async Task WithChannelLockAsync(
        SemaphoreSlim gate,
        CancellationToken ct,
        Func<Task> action
    )
    {
        await gate.WaitAsync(ct);
        try
        {
            await action();
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record PendingDelivery(
        TEvent Payload,
        ulong DeliveryTag,
        byte[] Body,
        IReadOnlyBasicProperties Properties
    );
}
