using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Services;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services;

public sealed class ResponseEmbeddingSyncTests
{
    [Fact]
    public async Task AcceptAsync_SchedulesRecruiterButNotResponder()
    {
        await using var db = CreateDbContext();
        var (recruiter, responder, response) = await SeedResponseAsync(db);
        var service = new ResponseService(db, new EmbeddingSyncScheduler(db));

        await service.AcceptAsync(response.Id, recruiter.Id, CancellationToken.None);

        var messages = await db.EmbeddingSyncOutbox.ToListAsync();
        var message = Assert.Single(messages);
        Assert.Equal(EmbeddingTarget.User, message.Target);
        Assert.Equal(recruiter.Id, message.EntityId);
        Assert.DoesNotContain(messages, item => item.EntityId == responder.Id);
    }

    [Fact]
    public async Task RejectAsync_DoesNotScheduleEmbeddingUpdate()
    {
        await using var db = CreateDbContext();
        var (recruiter, _, response) = await SeedResponseAsync(db);
        var service = new ResponseService(db, new EmbeddingSyncScheduler(db));

        await service.RejectAsync(response.Id, recruiter.Id, CancellationToken.None);

        Assert.Empty(await db.EmbeddingSyncOutbox.ToListAsync());
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

    private static async Task<(User Recruiter, User Responder, Response Response)> SeedResponseAsync(
        AppDbContext db
    )
    {
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var responder = new User("responder", "hash", UserRole.User);
        var game = new Game("game");
        var recruitment = new Recruitment(
            game,
            recruiter,
            "title",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var response = new Response(recruitment, responder);
        recruitment.Responses.Add(response);
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        return (recruiter, responder, response);
    }
}
