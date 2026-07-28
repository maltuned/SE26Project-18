using Microsoft.Extensions.Options;
using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class TagEmbeddingBuilder
{
    private readonly IEmbeddingService _embeddingService;
    private readonly int _dimension;

    public TagEmbeddingBuilder(
        IEmbeddingService embeddingService,
        IOptions<EmbeddingOptions> options
    )
    {
        _embeddingService = embeddingService;
        _dimension = options.Value.Dimension;
    }

    public async Task<ReadOnlyMemory<float>?> BuildAsync(
        IReadOnlyCollection<WeightedTagInput> tags,
        string category,
        CancellationToken ct
    )
    {
        var result = await BuildManyAsync(
            new Dictionary<long, IReadOnlyCollection<WeightedTagInput>> { [0] = tags },
            category,
            ct
        );
        return result[0];
    }

    public async Task<IReadOnlyDictionary<long, ReadOnlyMemory<float>?>> BuildManyAsync(
        IReadOnlyDictionary<long, IReadOnlyCollection<WeightedTagInput>> profiles,
        string category,
        CancellationToken ct
    )
    {
        var texts = profiles
            .Values.SelectMany(tags => tags)
            .Select(tag => $"{category}: {tag.Name}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var embeddings =
            texts.Length == 0
                ? new Dictionary<string, ReadOnlyMemory<float>>()
                : await _embeddingService.EmbedAsync(texts, ct);
        var result = new Dictionary<long, ReadOnlyMemory<float>?>(profiles.Count);

        foreach (var (id, tags) in profiles)
        {
            result[id] =
                tags.Count == 0
                    ? (ReadOnlyMemory<float>?)null
                    : WeightedEmbeddingAggregator.Aggregate(
                        tags.Select(tag => (embeddings[$"{category}: {tag.Name}"], tag.Weight))
                            .ToList(),
                        _dimension
                    );
        }

        return result;
    }
}

internal sealed record WeightedTagInput(long Id, string Name, double Weight);
