using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal sealed class RabbitMqEventConsumerHostedService<TEvent, TConsumer> : BackgroundService
    where TConsumer : class, IEventConsumer<TEvent>
{
    private readonly ConnectionFactory _connectionFactory;

    private readonly RabbitMqOptions _options;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly RabbitMqConsumerRegistration<TEvent, TConsumer> _registration;

    private readonly ILogger<RabbitMqEventConsumerHostedService<TEvent, TConsumer>> _logger;

    public RabbitMqEventConsumerHostedService(
        IOptions<RabbitMqOptions> options,
        IServiceScopeFactory scopeFactory,
        RabbitMqConsumerRegistration<TEvent, TConsumer> registration,
        ILogger<RabbitMqEventConsumerHostedService<TEvent, TConsumer>> logger
    )
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _registration = registration;
        _logger = logger;
        _connectionFactory = RabbitMqConnectionFactory.Create(
            _options,
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
                    "Failed to start RabbitMQ consumer {ConsumerName}; retrying in {RetryDelaySeconds} seconds",
                    typeof(TConsumer).Name,
                    _options.RecoveryDelaySeconds
                );
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.RecoveryDelaySeconds),
                    stoppingToken
                );
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var eventName = _registration.EventName;
        var queueName = _registration.QueueName;
        var deadLetterQueueName = $"{queueName}.dead-letter";
        var retryExchangeName = $"{_options.ExchangeName}.retry";
        var retryQueueName = $"{queueName}.retry";

        await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            cancellationToken: stoppingToken
        );

        await channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );
        await channel.ExchangeDeclareAsync(
            _options.DeadLetterExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );
        await channel.ExchangeDeclareAsync(
            retryExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );
        await channel.QueueDeclareAsync(
            deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-max-length"] = _options.DeadLetterQueueMaxLength,
            },
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            deadLetterQueueName,
            _options.DeadLetterExchangeName,
            deadLetterQueueName,
            cancellationToken: stoppingToken
        );
        await channel.QueueDeclareAsync(
            retryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = _options.RetryDelaySeconds * 1_000,
                ["x-dead-letter-exchange"] = _options.ExchangeName,
                ["x-dead-letter-routing-key"] = eventName,
            },
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            retryQueueName,
            retryExchangeName,
            queueName,
            cancellationToken: stoppingToken
        );
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _options.DeadLetterExchangeName,
                ["x-dead-letter-routing-key"] = deadLetterQueueName,
            },
            cancellationToken: stoppingToken
        );
        await channel.QueueBindAsync(
            queueName,
            _options.ExchangeName,
            eventName,
            cancellationToken: stoppingToken
        );
        await channel.BasicQosAsync(
            0,
            (ushort)_options.PrefetchCount,
            global: false,
            stoppingToken
        );

        var rabbitConsumer = new AsyncEventingBasicConsumer(channel);
        rabbitConsumer.ReceivedAsync += async (_, eventArgs) =>
        {
            TEvent payload;
            try
            {
                var deserializedPayload = JsonSerializer.Deserialize<TEvent>(
                    eventArgs.Body.Span,
                    EventJsonSerializer.Options
                );
                if (deserializedPayload is null)
                    throw new JsonException("Event payload cannot be null.");

                payload = deserializedPayload;
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                _logger.LogError(
                    exception,
                    "Invalid event {EventName} received from queue {QueueName}",
                    eventName,
                    queueName
                );
                await DeadLetterAsync(channel, eventArgs.DeliveryTag, eventName, queueName);
                return;
            }

            try
            {
                await using var messageScope = _scopeFactory.CreateAsyncScope();
                var messageConsumer = messageScope.ServiceProvider.GetRequiredService<TConsumer>();
                await messageConsumer.ConsumeAsync(payload, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The broker requeues unacknowledged messages when the channel closes.
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to consume event {EventName} from queue {QueueName}",
                    eventName,
                    queueName
                );
                await RetryOrDeadLetterAsync(
                    channel,
                    eventArgs,
                    retryExchangeName,
                    queueName,
                    eventName,
                    stoppingToken
                );
                return;
            }

            try
            {
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // The broker requeues unacknowledged messages when the channel closes.
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to acknowledge event {EventName} from queue {QueueName}; it may be redelivered",
                    eventName,
                    queueName
                );
            }
        };

        await channel.BasicConsumeAsync(
            queueName,
            autoAck: false,
            consumer: rabbitConsumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        string retryExchangeName,
        string queueName,
        string eventName,
        CancellationToken ct
    )
    {
        var retryCount = GetRetryCount(eventArgs.BasicProperties.Headers);
        if (retryCount >= _options.MaxRetryAttempts)
        {
            await DeadLetterAsync(channel, eventArgs.DeliveryTag, eventName, queueName);
            return;
        }

        var headers = eventArgs.BasicProperties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(eventArgs.BasicProperties.Headers);
        headers["x-retry-count"] = retryCount + 1;

        try
        {
            _logger.LogWarning(
                "Scheduling retry {RetryAttempt} for event {EventName} from queue {QueueName}",
                retryCount + 1,
                eventName,
                queueName
            );
            await channel.BasicPublishAsync(
                retryExchangeName,
                queueName,
                mandatory: false,
                new BasicProperties
                {
                    ContentType = eventArgs.BasicProperties.ContentType,
                    ContentEncoding = eventArgs.BasicProperties.ContentEncoding,
                    DeliveryMode = eventArgs.BasicProperties.DeliveryMode,
                    MessageId = eventArgs.BasicProperties.MessageId,
                    CorrelationId = eventArgs.BasicProperties.CorrelationId,
                    Timestamp = eventArgs.BasicProperties.Timestamp,
                    Headers = headers,
                },
                eventArgs.Body,
                ct
            );
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, ct);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to schedule retry {RetryAttempt} for event {EventName} from queue {QueueName}",
                retryCount + 1,
                eventName,
                queueName
            );
        }
    }

    private static int GetRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue("x-retry-count", out var value))
            return 0;

        return value switch
        {
            byte retryCount => retryCount,
            short retryCount => retryCount,
            int retryCount => retryCount,
            long retryCount when retryCount <= int.MaxValue => (int)retryCount,
            _ => 0,
        };
    }

    private async Task DeadLetterAsync(
        IChannel channel,
        ulong deliveryTag,
        string eventName,
        string queueName
    )
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to dead-letter event {EventName} from queue {QueueName}",
                eventName,
                queueName
            );
        }
    }
}
