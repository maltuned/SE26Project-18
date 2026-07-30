using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Services;
using SE26Project_18.Api.Models.Exceptions;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Tests.Services;

public sealed class ResponseServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesResponseAndChat()
    {
        await using var db = CreateDbContext();
        var (recruiter, responder, recruitment) = await SeedRecruitmentAsync(db);
        var service = CreateService(db);

        var result = await service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None);

        Assert.Equal(responder.Id, result.ResponderId);
        Assert.Equal(recruitment.Id, result.RecruitmentId);
        Assert.Equal("Pending", result.Type.ToString());
        Assert.Single(db.Responses);
        Assert.Single(db.Chats);
    }

    [Fact]
    public async Task CreateAsync_RejectsOwnRecruitment()
    {
        await using var db = CreateDbContext();
        var (recruiter, _, recruitment) = await SeedRecruitmentAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(recruiter.Id, recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicate()
    {
        await using var db = CreateDbContext();
        var (_, responder, recruitment) = await SeedRecruitmentAsync(db);
        var service = CreateService(db);
        await service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateAsync_RejectsClosedRecruitment()
    {
        await using var db = CreateDbContext();
        var (_, responder, recruitment) = await SeedRecruitmentAsync(db);
        recruitment.Status = RecruitmentStatus.Closed;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateAsync_RejectsExpiredRecruitment()
    {
        await using var db = CreateDbContext();
        var (_, responder, recruitment) = await SeedRecruitmentAsync(db);
        recruitment.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CreateAsync_RejectsFullRecruitment()
    {
        await using var db = CreateDbContext();
        var (_, responder, recruitment) = await SeedRecruitmentAsync(db);
        recruitment.AddParticipant();
        recruitment.AddParticipant();
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(responder.Id, recruitment.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task AcceptAsync_AcceptsResponseAndIncrementsParticipants()
    {
        await using var db = CreateDbContext();
        var (recruiter, responder) = await SeedResponseAsync(db);
        var service = CreateService(db);

        var result = await service.AcceptAsync(db.Responses.First().Id, recruiter.Id, CancellationToken.None);

        Assert.Equal("Accepted", result.Type.ToString());
        Assert.Equal(1, db.Responses.First().Recruitment.CurrParticipants);
    }

    [Fact]
    public async Task AcceptAsync_RejectsNonRecruiter()
    {
        await using var db = CreateDbContext();
        var (_, responder) = await SeedResponseAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.AcceptAsync(db.Responses.First().Id, responder.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task AcceptAsync_RejectsAlreadyProcessed()
    {
        await using var db = CreateDbContext();
        var (recruiter, _) = await SeedResponseAsync(db);
        var service = CreateService(db);
        var responseId = db.Responses.First().Id;
        await service.RejectAsync(responseId, recruiter.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ResponseAlreadyProcessedException>(() =>
            service.AcceptAsync(responseId, recruiter.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task AcceptAsync_ClosesRecruitmentWhenFull()
    {
        await using var db = CreateDbContext();
        var (recruiter, _) = await SeedResponseAsync(db);
        var recruitment = db.Recruitments.First();
        recruitment.AddParticipant();
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.AcceptAsync(db.Responses.First().Id, recruiter.Id, CancellationToken.None);

        Assert.Equal(RecruitmentStatus.Closed, recruitment.Status);
    }

    [Fact]
    public async Task RejectAsync_RejectsResponse()
    {
        await using var db = CreateDbContext();
        var (recruiter, _) = await SeedResponseAsync(db);
        var service = CreateService(db);

        var result = await service.RejectAsync(db.Responses.First().Id, recruiter.Id, CancellationToken.None);

        Assert.Equal("Rejected", result.Type.ToString());
    }

    [Fact]
    public async Task RejectAsync_RejectsNonRecruiter()
    {
        await using var db = CreateDbContext();
        var (_, responder) = await SeedResponseAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.RejectAsync(db.Responses.First().Id, responder.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsResponseForParticipant()
    {
        await using var db = CreateDbContext();
        var (recruiter, responder) = await SeedResponseAsync(db);
        var service = CreateService(db);
        var responseId = db.Responses.First().Id;

        var recruiterView = await service.GetByIdAsync(responseId, recruiter.Id, CancellationToken.None);
        var responderView = await service.GetByIdAsync(responseId, responder.Id, CancellationToken.None);

        Assert.Equal(responseId, recruiterView.Id);
        Assert.Equal(responseId, responderView.Id);
    }

    [Fact]
    public async Task GetByIdAsync_RejectsNonParticipant()
    {
        await using var db = CreateDbContext();
        await SeedResponseAsync(db);
        var outsider = new User("outsider", "hash", UserRole.User);
        db.Users.Add(outsider);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetByIdAsync(db.Responses.First().Id, outsider.Id, CancellationToken.None)
        );
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFoundForMissingResponse()
    {
        await using var db = CreateDbContext();
        var (recruiter, _) = await SeedResponseAsync(db);
        var service = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetByIdAsync(9999, recruiter.Id, CancellationToken.None)
        );
    }

    private static ResponseService CreateService(AppDbContext db)
    {
        return new ResponseService(db, new EmbeddingSyncScheduler(db));
    }

    private static AppDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<(User Recruiter, User Responder, Recruitment Recruitment)> SeedRecruitmentAsync(
        AppDbContext db
    )
    {
        var recruiter = new User("recruiter", "hash", UserRole.User);
        var responder = new User("responder", "hash", UserRole.User);
        var game = new Game("game");
        var recruitment = new Recruitment(
            game,
            recruiter,
            "test",
            2,
            DateTime.UtcNow.AddDays(1)
        );
        db.Users.AddRange(recruiter, responder);
        db.Recruitments.Add(recruitment);
        await db.SaveChangesAsync();
        return (recruiter, responder, recruitment);
    }

    private static async Task<(User Recruiter, User Responder)> SeedResponseAsync(
        AppDbContext db
    )
    {
        var (recruiter, responder, recruitment) = await SeedRecruitmentAsync(db);
        var response = new Response(recruitment, responder);
        recruitment.Responses.Add(response);
        await db.SaveChangesAsync();
        return (recruiter, responder);
    }
}
