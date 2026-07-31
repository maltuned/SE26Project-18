using System.ComponentModel.DataAnnotations.Schema;
using SE26Project_18.Backend.Infrastructure.Embedding;

namespace SE26Project_18.Backend.Models.Entities;

[Table("embedding_sync_outbox")]
public sealed class EmbeddingSyncOutboxMessage
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

    public EmbeddingSyncRequested ToEvent() => new(EventId, Target, EntityId, Id);
}
