namespace SE26Project_18.Api.Infrastructure.Embedding;

internal sealed record EmbeddingSyncRequested(
    Guid EventId,
    EmbeddingTarget Target,
    long EntityId,
    long Version
)
{
    public const string EventName = "embedding.sync.requested.v1";
    public const string QueueName = "se26project-18.embedding-sync.v1";
}

internal enum EmbeddingTarget
{
    User,
    Game,
    Recruitment,
}
