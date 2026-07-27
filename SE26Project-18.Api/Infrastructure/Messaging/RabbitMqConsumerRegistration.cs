namespace SE26Project_18.Api.Infrastructure.Messaging;

internal sealed record RabbitMqConsumerRegistration<TEvent, TConsumer>(
    string EventName,
    string QueueName
)
    where TConsumer : class, IEventConsumer<TEvent>;
