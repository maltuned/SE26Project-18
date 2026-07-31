using SE26Project_18.Backend.Infrastructure.Embedding;

namespace SE26Project_18.Backend.Services.Recommendations;

public interface IEmbeddingSyncScheduler
{
    void Schedule(EmbeddingTarget target, long entityId);

    void Schedule(EmbeddingTarget target, IEnumerable<long> entityIds);
}
