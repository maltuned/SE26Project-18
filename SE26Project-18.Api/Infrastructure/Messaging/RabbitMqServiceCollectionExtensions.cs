namespace SE26Project_18.Api.Infrastructure.Messaging;

internal static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqConsumer<TEvent, TConsumer>(
        this IServiceCollection services,
        string eventName,
        string queueName
    )
        where TConsumer : class, IEventConsumer<TEvent>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        services.AddScoped<TConsumer>();
        services.AddSingleton(
            new RabbitMqConsumerRegistration<TEvent, TConsumer>(eventName, queueName)
        );
        services.AddHostedService<RabbitMqEventConsumerHostedService<TEvent, TConsumer>>();

        return services;
    }
}
