namespace SE26Project_18.Api.Infrastructure.Messaging;

internal interface IBatchEventConsumer<in TEvent>
{
    Task ConsumeAsync(IReadOnlyCollection<TEvent> events, CancellationToken ct);
}
