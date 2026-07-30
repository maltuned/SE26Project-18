namespace SE26Project_18.Backend.Infrastructure.Messaging;

internal interface IBatchEventConsumer<in TEvent>
{
    Task ConsumeAsync(IReadOnlyCollection<TEvent> events, CancellationToken ct);
}
