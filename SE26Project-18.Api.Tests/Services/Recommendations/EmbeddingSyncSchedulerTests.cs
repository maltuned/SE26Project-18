using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services.Recommendations;

public sealed class EmbeddingSyncSchedulerTests
{
    [Fact]
    public async Task Schedule_DeduplicatesTargetWithinUnitOfWork()
    {
        await using var db = CreateDbContext();
        var scheduler = new EmbeddingSyncScheduler(db);

        scheduler.Schedule(EmbeddingTarget.User, 42);
        scheduler.Schedule(EmbeddingTarget.User, 42);
        await db.SaveChangesAsync();

        var message = Assert.Single(await db.EmbeddingSyncOutbox.ToListAsync());
        Assert.Equal(EmbeddingTarget.User, message.Target);
        Assert.Equal(42, message.EntityId);
        Assert.True(message.Id > 0);
        Assert.Equal(message.Id, message.ToEvent().Version);
    }

    private static AppDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
