using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SE26Project_18.Api.Infrastructure.Messaging;

internal sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;

    private readonly RabbitMqOptions _options;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly ILogger<RabbitMqEventPublisher> _logger;

    private IConnection? _connection;

    private IChannel? _channel;

    private int _disposeStarted;

    public RabbitMqEventPublisher(
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventPublisher> logger
    )
    {
        _options = options.Value;
        _logger = logger;
        _connectionFactory = RabbitMqConnectionFactory.Create(
            _options,
            "se26project-18:event-publisher",
            automaticRecoveryEnabled: false
        );
    }

    public async Task PublishAsync<T>(string eventName, T payload, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(payload);

        await _gate.WaitAsync(ct);
        try
        {
            ThrowIfDisposed();

            var body = JsonSerializer.SerializeToUtf8Bytes(payload, EventJsonSerializer.Options);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString("N"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };
            var channel = await GetChannelAsync(ct);
            await channel.BasicPublishAsync(
                _options.ExchangeName,
                eventName,
                mandatory: true,
                properties,
                body,
                ct
            );
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish event {EventName} to exchange {ExchangeName}",
                eventName,
                _options.ExchangeName
            );
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
            return;

        await _gate.WaitAsync();
        try
        {
            await DisposeResourcesAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_connection is not { IsOpen: true })
        {
            await DisposeResourcesAsync();
            _connection = await _connectionFactory.CreateConnectionAsync(ct);
        }

        if (_channel is { IsOpen: true })
            return _channel;

        if (_channel is not null)
            await _channel.DisposeAsync();

        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true
            ),
            ct
        );
        await _channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct
        );

        return _channel;
    }

    private async Task DisposeResourcesAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposeStarted) != 0, this);
    }
}
