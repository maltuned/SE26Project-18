namespace SE26Project_18.Api.Infrastructure.Messaging;

internal interface IEventConsumer<in TEvent>
{
    Task ConsumeAsync(TEvent payload, CancellationToken ct);
}
