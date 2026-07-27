namespace SE26Project_18.Api.Infrastructure.Messaging;

internal interface IEventPublisher
{
    Task PublishAsync<T>(string eventName, T payload, CancellationToken ct);
}
