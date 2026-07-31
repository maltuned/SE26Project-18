namespace SE26Project_18.Backend.Infrastructure.Messaging;

internal sealed record RabbitMqBatchConsumerRegistration<TEvent, TConsumer>(
    string EventName,
    string QueueName
)
    where TConsumer : class, IBatchEventConsumer<TEvent>;
