namespace SE26Project_18.Backend.Infrastructure.VectorStore;

internal sealed class RecommendationVectorStoreReadiness
{
    private readonly TaskCompletionSource _ready = new(
        TaskCreationOptions.RunContinuationsAsynchronously);

    public void MarkReady() => _ready.TrySetResult();

    public Task WaitAsync(CancellationToken ct) => _ready.Task.WaitAsync(ct);
}
