namespace SE26Project_18.Backend.Infrastructure.Embedding;

internal interface IEmbeddingService
{
    Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyCollection<string> texts,
        CancellationToken ct
    );
}
