using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SE26Project_18.Api.Services;

/// <summary>
/// RabbitMQ 消息队列服务 — 管理连接、发布和消费
/// </summary>
public sealed class RabbitMQService : IDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private const string ExchangeName = "pairing_tool";

    /// <summary>
    /// 构造函数 — 建立 RabbitMQ 连接并声明交换机，不可用时静默降级
    /// </summary>
    public RabbitMQService(IConfiguration config)
    {
        var host = config["RabbitMQ:Host"] ?? "localhost";
        var port = int.Parse(config["RabbitMQ:Port"] ?? "5672");
        var username = config["RabbitMQ:Username"] ?? "guest";
        var password = config["RabbitMQ:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
        };

        try
        {
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            ).GetAwaiter().GetResult();
        }
        catch
        {
            _connection = null;
            _channel = null;
        }
    }

    /// <summary>
    /// 发布消息到指定路由键（如 "user.registered"），RabbitMQ 不可用时静默跳过
    /// </summary>
    public async Task PublishAsync(string routingKey, object message)
    {
        if (_channel is null) return;

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                body: body
            );
        }
        catch
        {
            // 发布失败不影响主流程
        }
    }

    /// <summary>
    /// 声明队列并绑定到路由键，注册异步消费回调
    /// </summary>
    public async Task SubscribeAsync(string queueName, string routingKey, Func<string, Task> handler)
    {
        if (_channel is null) return;

        try
        {
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: ExchangeName,
                routingKey: routingKey
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                await handler(body);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            };

            await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
        }
        catch
        {
            // 订阅失败不影响主流程
        }
    }

    /// <summary>
    /// 释放连接和通道
    /// </summary>
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
