using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Services.Recommendations;

internal interface IEmbeddingSyncScheduler
{
    void Schedule(EmbeddingTarget target, long entityId);

    void Schedule(EmbeddingTarget target, IEnumerable<long> entityIds);
}
