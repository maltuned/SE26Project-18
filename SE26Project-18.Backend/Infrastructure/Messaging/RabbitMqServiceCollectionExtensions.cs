namespace SE26Project_18.Backend.Infrastructure.Messaging;

internal static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqBatchConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        string eventName,
        string queueName
    )
        where TConsumer : class, IBatchEventConsumer<TEvent>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        services.AddScoped<TConsumer>();
        services.AddSingleton(
            new RabbitMqBatchConsumerRegistration<TEvent, TConsumer>(eventName, queueName)
        );
        services.AddHostedService<RabbitMqBatchEventConsumerHostedService<TEvent, TConsumer>>();

        return services;
    }
}
