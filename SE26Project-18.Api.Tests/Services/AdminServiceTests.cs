using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Services;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services;

public sealed class AdminServiceTests
{
    [Fact]
    public async Task SetSuspensionAsync_SuspendsUserAndRevokesRefreshTokens()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.RefreshTokens.Add(
            new RefreshToken(user.Id, "token", DateTime.UtcNow.AddDays(1))
        );
        await db.SaveChangesAsync();
        var service = new UserService(db, new EmbeddingSyncScheduler(db));

        var response = await service.SetSuspensionAsync(
            user.Id,
            new SetUserSuspensionRequest(true),
            CancellationToken.None
        );

        Assert.Equal(UserStatus.Suspended, response.Status);
        Assert.All(await db.RefreshTokens.ToListAsync(), token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public async Task SetSuspensionAsync_UnsuspendsUserAsOffline()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User) { Status = UserStatus.Suspended };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new UserService(db, new EmbeddingSyncScheduler(db));

        var response = await service.SetSuspensionAsync(
            user.Id,
            new SetUserSuspensionRequest(false),
            CancellationToken.None
        );

        Assert.Equal(UserStatus.Offline, response.Status);
    }

    [Fact]
    public async Task ForceCloseAsync_DeletesRecruitmentAndSchedulesEmbedding()
    {
        await using var db = CreateDbContext();
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var recruitment = new Recruitment(
            new Game("game"),
            recruiter,
            "title",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        var service = CreateRecruitmentService(db);

        await service.ForceCloseAsync(recruitment.Id, CancellationToken.None);

        Assert.Equal(RecruitmentStatus.Deleted, recruitment.Status);
        Assert.Equal(1, recruitment.Version);
        var message = Assert.Single(await db.EmbeddingSyncOutbox.ToListAsync());
        Assert.Equal(EmbeddingTarget.Recruitment, message.Target);
        Assert.Equal(recruitment.Id, message.EntityId);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task ForceCloseAsync_RejectsDeletedRecruitment()
    {
        await using var db = CreateDbContext();
        var recruitment = new Recruitment(
            new Game("game"),
            new User("recruiter", "hash", UserRole.User),
            "title",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        recruitment.Delete();
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        var service = CreateRecruitmentService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.ForceCloseAsync(recruitment.Id, CancellationToken.None)
        );
    }

    private static RecruitmentService CreateRecruitmentService(AppDbContext db)
    {
        return new RecruitmentService(
            db,
            new PassthroughRecommendationAlgorithm(),
            new EmbeddingSyncScheduler(db)
        );
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

    private sealed class PassthroughRecommendationAlgorithm
        : IRecruitmentRecommendationAlgorithm
    {
        public Task<IReadOnlyList<long>> RankAsync(
            long userId,
            IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
            CancellationToken ct
        )
        {
            return Task.FromResult<IReadOnlyList<long>>(
                candidates.Select(candidate => candidate.Id).ToList()
            );
        }
    }
}
