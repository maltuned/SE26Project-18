namespace SE26Project_18.Backend.Infrastructure.Embedding;

public sealed record EmbeddingSyncRequested(
    Guid EventId,
    EmbeddingTarget Target,
    long EntityId,
    long Version
)
{
    public const string EventName = "embedding.sync.requested.v1";

    public const string QueueName = "se26project_18.embedding-sync.v1";
}

public enum EmbeddingTarget
{
    User,
    Game,
    Recruitment,
}
