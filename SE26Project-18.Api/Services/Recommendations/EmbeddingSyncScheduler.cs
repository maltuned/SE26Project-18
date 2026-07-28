using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Services.Recommendations;

internal sealed class EmbeddingSyncScheduler : IEmbeddingSyncScheduler
{
    private readonly AppDbContext _db;

    public EmbeddingSyncScheduler(AppDbContext db)
    {
        _db = db;
    }

    public void Schedule(EmbeddingTarget target, long entityId)
    {
        if (
            entityId <= 0
            || _db.ChangeTracker.Entries<EmbeddingSyncOutboxMessage>()
                .Any(entry =>
                    entry.State == EntityState.Added
                    && entry.Entity.Target == target
                    && entry.Entity.EntityId == entityId
                )
        )
            return;

        _db.EmbeddingSyncOutbox.Add(new EmbeddingSyncOutboxMessage(target, entityId));
    }

    public void Schedule(EmbeddingTarget target, IEnumerable<long> entityIds)
    {
        foreach (var entityId in entityIds.Distinct())
            Schedule(target, entityId);
    }
}
