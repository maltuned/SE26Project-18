using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SE26Project_18.Backend.Data;

namespace SE26Project_18.Backend.Tests;

/// <summary>
/// Helper to create an InMemory AppDbContext for unit tests.
/// Each test gets a unique database name to ensure isolation.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    public static AppDbContext CreateWithSeed(Action<AppDbContext> seed)
    {
        var db = Create();
        seed(db);
        db.SaveChanges();
        return db;
    }
}
