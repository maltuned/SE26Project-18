using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Models.Entities;

[Table("embedding_sync_outbox")]
internal sealed class EmbeddingSyncOutboxMessage
{
    public long Id { get; private set; }

    public Guid EventId { get; private init; } = Guid.NewGuid();

    public EmbeddingTarget Target { get; private init; }

    public long EntityId { get; private init; }

    public DateTime CreatedAt { get; private init; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; private set; }

    public Guid? LeaseId { get; set; }

    public DateTime? LeaseExpiresAt { get; set; }

    public int PublishAttempts { get; private set; }

    public string? LastError { get; private set; }

    public EmbeddingSyncOutboxMessage(EmbeddingTarget target, long entityId)
    {
        Target = target;
        EntityId = entityId;
    }

    public EmbeddingSyncRequested ToEvent()
    {
        return new EmbeddingSyncRequested(EventId, Target, EntityId, Id);
    }

    public void MarkPublished()
    {
        PublishedAt = DateTime.UtcNow;
        LeaseId = null;
        LeaseExpiresAt = null;
        LastError = null;
    }

    public void MarkPublishFailed(string error, DateTime retryAt)
    {
        PublishAttempts++;
        LastError = error.Length <= 2_000 ? error : error[..2_000];
        LeaseId = null;
        LeaseExpiresAt = retryAt;
    }
}
