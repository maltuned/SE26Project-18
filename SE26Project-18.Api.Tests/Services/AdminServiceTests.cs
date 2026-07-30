using System.Net.WebSockets;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.Realtime;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
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
        var connections = new RecordingConnectionManager(db);
        var service = CreateUserService(db, connections);

        var response = await service.SetSuspensionAsync(
            999,
            user.Id,
            new SetUserSuspensionRequest(true),
            CancellationToken.None
        );

        Assert.Equal(UserStatus.Suspended, response.Status);
        Assert.All(await db.RefreshTokens.ToListAsync(), token => Assert.True(token.IsRevoked));
        Assert.Equal(user.Id, connections.ClosedUserId);
        Assert.False(connections.HadPendingChangesWhenClosed);
    }

    [Fact]
    public async Task SetSuspensionAsync_UnsuspendsUserAsOffline()
    {
        await using var db = CreateDbContext();
        var user = new User("user", "hash", UserRole.User) { Status = UserStatus.Suspended };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var connections = new RecordingConnectionManager(db);
        var service = CreateUserService(db, connections);

        var response = await service.SetSuspensionAsync(
            999,
            user.Id,
            new SetUserSuspensionRequest(false),
            CancellationToken.None
        );

        Assert.Equal(UserStatus.Offline, response.Status);
        Assert.Null(connections.ClosedUserId);
    }

    [Fact]
    public async Task SetSuspensionAsync_RejectsSelfSuspension()
    {
        await using var db = CreateDbContext();
        var admin = new User("admin", "hash", UserRole.Admin);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var service = CreateUserService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SetSuspensionAsync(
                admin.Id,
                admin.Id,
                new SetUserSuspensionRequest(true),
                CancellationToken.None
            )
        );

        Assert.Equal(UserStatus.Online, admin.Status);
    }

    [Fact]
    public async Task SetSuspensionAsync_RejectsSuspendingOrUnsuspendingAdmin()
    {
        await using var db = CreateDbContext();
        var actor = new User("actor", "hash", UserRole.Admin);
        var target = new User("target", "hash", UserRole.Admin);
        db.Users.AddRange(actor, target);
        await db.SaveChangesAsync();
        var service = CreateUserService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SetSuspensionAsync(
                actor.Id,
                target.Id,
                new SetUserSuspensionRequest(true),
                CancellationToken.None
            )
        );
        target.Status = UserStatus.Suspended;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.SetSuspensionAsync(
                actor.Id,
                target.Id,
                new SetUserSuspensionRequest(false),
                CancellationToken.None
            )
        );
    }

    [Fact]
    public async Task GetUsersAsync_FiltersAndPaginatesWithAdminVisibility()
    {
        await using var db = CreateDbContext();
        db.Users.AddRange(
            new User("admin-one", "hash", UserRole.Admin),
            new User("admin-two", "hash", UserRole.Admin) { Status = UserStatus.Suspended },
            new User("normal", "hash", UserRole.User)
        );
        await db.SaveChangesAsync();
        var service = new AdminService(db);

        var page = await service.GetUsersAsync(
            new AdminUserQueryRequest("admin", null, true, 2, 1),
            CancellationToken.None
        );
        var suspended = await service.GetUsersAsync(
            new AdminUserQueryRequest(null, UserStatus.Suspended, null),
            CancellationToken.None
        );
        var normal = await service.GetUsersAsync(
            new AdminUserQueryRequest(null, null, false),
            CancellationToken.None
        );

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("admin-two", Assert.Single(page.Items).Username);
        Assert.True(Assert.Single(page.Items).IsAdmin);
        Assert.Equal("admin-two", Assert.Single(suspended.Items).Username);
        Assert.False(Assert.Single(normal.Items).IsAdmin);
    }

    [Fact]
    public async Task GetGamesAsync_FiltersAndUsesStablePages()
    {
        await using var db = CreateDbContext();
        db.Games.AddRange(new Game("Alpha"), new Game("Beta"), new Game("Gamma"));
        await db.SaveChangesAsync();
        var service = new AdminService(db);

        var page = await service.GetGamesAsync(
            new AdminGameQueryRequest("a", 2, 2),
            CancellationToken.None
        );

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal("Gamma", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task GetGamesAsync_MaximumAcceptedPageDoesNotOverflow()
    {
        await using var db = CreateDbContext();
        var service = new AdminService(db);

        var page = await service.GetGamesAsync(
            new AdminGameQueryRequest(null, 21_474_837, 100),
            CancellationToken.None
        );

        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task GetRecruitmentsAsync_AppliesFiltersAndIncludesDeletedWithAllResponses()
    {
        await using var db = CreateDbContext();
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var otherRecruiter = new User("other", "hash", UserRole.User);
        var responder1 = new User("responder1", "hash", UserRole.User);
        var responder2 = new User("responder2", "hash", UserRole.User);
        var game = new Game("Target Game");
        var otherGame = new Game("Other Game");
        var deleted = new Recruitment(
            game,
            recruiter,
            "Target recruitment",
            3,
            DateTime.UtcNow.AddDays(1)
        );
        deleted.Responses.Add(new Response(deleted, responder1));
        deleted.Responses.Add(new Response(deleted, responder2));
        deleted.Delete();
        db.Recruitments.AddRange(
            deleted,
            new Recruitment(otherGame, otherRecruiter, "Unrelated", 2, DateTime.UtcNow.AddDays(1))
        );
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new AdminService(db);

        var all = await service.GetRecruitmentsAsync(
            new AdminRecruitmentQueryRequest("Target", recruiter.Id, game.Id, null),
            CancellationToken.None
        );
        var deletedOnly = await service.GetRecruitmentsAsync(
            new AdminRecruitmentQueryRequest(
                null,
                null,
                null,
                RecruitmentStatus.Deleted
            ),
            CancellationToken.None
        );

        var item = Assert.Single(all.Items);
        Assert.Equal(RecruitmentStatus.Deleted, item.Status);
        Assert.Equal(2, item.Responses.Count);
        Assert.Equal(deleted.Id, Assert.Single(deletedOnly.Items).Id);
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

    [Fact]
    public async Task RecruitmentQueries_ReturnAllResponsesUsingScalarIds()
    {
        await using var db = CreateDbContext();
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var responder1 = new User("responder1", "hash", UserRole.User);
        var responder2 = new User("responder2", "hash", UserRole.User);
        var viewer = new User("viewer", "hash", UserRole.User);
        var recruitment = new Recruitment(
            new Game("game"),
            recruiter,
            "title",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        recruitment.Responses.Add(new Response(recruitment, responder1));
        recruitment.Responses.Add(new Response(recruitment, responder2));
        db.AddRange(recruitment, viewer);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = CreateRecruitmentService(db);

        var byId = await service.GetByIdAsync(recruitment.Id, CancellationToken.None);
        var byRecruiter = await service.GetByRecruiterAsync(
            recruiter.Id,
            1,
            20,
            null,
            CancellationToken.None
        );
        var search = await service.SearchAsync(
            viewer.Id,
            new RecruitmentQueryRequest(null, null, null),
            CancellationToken.None
        );

        AssertResponses(byId, recruitment.Id, responder1.Id, responder2.Id);
        AssertResponses(
            Assert.Single(byRecruiter.Items),
            recruitment.Id,
            responder1.Id,
            responder2.Id
        );
        AssertResponses(Assert.Single(search.Items), recruitment.Id, responder1.Id, responder2.Id);
    }

    [Fact]
    public async Task RecruitmentSearch_FallsBackToNewestFirstWhenRecommendationsAreUnavailable()
    {
        await using var db = CreateDbContext();
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var viewer = new User("viewer", "hash", UserRole.User);
        var game = new Game("game");
        var older = new Recruitment(
            game,
            recruiter,
            "older",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        var newer = new Recruitment(
            game,
            recruiter,
            "newer",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        db.AddRange(older, newer, viewer);
        await db.SaveChangesAsync();
        var service = CreateRecruitmentService(db, new UnavailableRecommendationAlgorithm());

        var result = await service.SearchAsync(
            viewer.Id,
            new RecruitmentQueryRequest(null, null, null),
            CancellationToken.None
        );

        Assert.Equal([newer.Id, older.Id], result.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetByRecruiterAsync_OptionalStatusFiltersServerSide()
    {
        await using var db = CreateDbContext();
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var game = new Game("game");
        var open = new Recruitment(game, recruiter, "open", 2, DateTime.UtcNow.AddDays(1));
        var deleted = new Recruitment(game, recruiter, "deleted", 2, DateTime.UtcNow.AddDays(1));
        deleted.Delete();
        db.AddRange(open, deleted);
        await db.SaveChangesAsync();
        var service = CreateRecruitmentService(db);

        var defaultPage = await service.GetByRecruiterAsync(
            recruiter.Id,
            1,
            20,
            null,
            CancellationToken.None
        );
        var deletedPage = await service.GetByRecruiterAsync(
            recruiter.Id,
            1,
            20,
            RecruitmentStatus.Deleted,
            CancellationToken.None
        );

        Assert.Equal(open.Id, Assert.Single(defaultPage.Items).Id);
        Assert.Equal(deleted.Id, Assert.Single(deletedPage.Items).Id);
    }

    private static void AssertResponses(
        RecruitmentResponse response,
        long recruitmentId,
        params long[] responderIds
    )
    {
        Assert.Equal(
            responderIds.Order(),
            response.Responses.Select(item => item.ResponderId).Order()
        );
        Assert.All(response.Responses, item => Assert.Equal(recruitmentId, item.RecruitmentId));
    }

    private static RecruitmentService CreateRecruitmentService(
        AppDbContext db,
        IRecruitmentRecommendationAlgorithm? recommendationAlgorithm = null
    )
    {
        return new RecruitmentService(
            db,
            recommendationAlgorithm ?? new PassthroughRecommendationAlgorithm(),
            new EmbeddingSyncScheduler(db),
            NullLogger<RecruitmentService>.Instance
        );
    }

    private static UserService CreateUserService(
        AppDbContext db,
        IMessageConnectionManager? connections = null
    )
    {
        return new UserService(
            db,
            new EmbeddingSyncScheduler(db),
            connections ?? new RecordingConnectionManager(db)
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

    private sealed class UnavailableRecommendationAlgorithm
        : IRecruitmentRecommendationAlgorithm
    {
        public Task<IReadOnlyList<long>> RankAsync(
            long userId,
            IReadOnlyCollection<RecruitmentRecommendationCandidate> candidates,
            CancellationToken ct
        )
        {
            throw new ServiceUnavailableException("Embedding service unavailable.");
        }
    }

    private sealed class RecordingConnectionManager(AppDbContext db) : IMessageConnectionManager
    {
        public long? ClosedUserId { get; private set; }

        public bool HadPendingChangesWhenClosed { get; private set; }

        public bool Add(long chatId, long userId, WebSocket socket) => true;

        public void Remove(long chatId, WebSocket socket) { }

        public Task BroadcastAsync(
            long chatId,
            MessageResponse message,
            CancellationToken ct
        ) => Task.CompletedTask;

        public Task CloseUserAsync(long userId)
        {
            ClosedUserId = userId;
            HadPendingChangesWhenClosed = db.ChangeTracker.HasChanges();
            return Task.CompletedTask;
        }

        public void AllowUser(long userId) { }

        public Task CloseAsync(
            long chatId,
            WebSocket socket,
            WebSocketCloseStatus closeStatus,
            string description
        ) => Task.CompletedTask;
    }
}
