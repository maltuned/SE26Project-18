using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class FeedbackServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();

    [Fact]
    public async Task SubmitFeedback_CreatesAndSaves()
    {
        var db = CreateDb();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        var service = new FeedbackService(db);

        await service.SubmitFeedbackAsync(user.Id, FeedbackType.ContentFeedback, "Great app");

        var feedback = db.Feedbacks.First();
        Assert.Equal(user.Id, feedback.UserId);
        Assert.Equal(FeedbackType.ContentFeedback, feedback.Type);
        Assert.Equal("Great app", feedback.Content);
        Assert.Equal(FeedbackStatus.Pending, feedback.Status);
    }

    [Fact]
    public async Task GetAll_ReturnsAll()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        var f1 = new Feedback { UserId = 1, Type = FeedbackType.ContentFeedback, Content = "c1" };
        var f2 = new Feedback { UserId = 1, Type = FeedbackType.ExperienceFeedback, Content = "c2" };
        db.Feedbacks.AddRange(f1, f2);
        await db.SaveChangesAsync();
        var service = new FeedbackService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByStatus()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        db.Feedbacks.Add(new Feedback { UserId = 1, Type = FeedbackType.ContentFeedback, Content = "c", Status = FeedbackStatus.Pending });
        db.Feedbacks.Add(new Feedback { UserId = 1, Type = FeedbackType.ContentFeedback, Content = "d", Status = FeedbackStatus.Resolved });
        await db.SaveChangesAsync();
        var service = new FeedbackService(db);

        var pending = await service.GetAllAsync(FeedbackStatus.Pending);
        var resolved = await service.GetAllAsync(FeedbackStatus.Resolved);

        Assert.Single(pending);
        Assert.Single(resolved);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenNotFound()
    {
        var db = CreateDb();
        var service = new FeedbackService(db);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ReturnsFeedback_WhenFound()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        var f = new Feedback { UserId = 1, Type = FeedbackType.ContentFeedback, Content = "test" };
        db.Feedbacks.Add(f);
        await db.SaveChangesAsync();
        var service = new FeedbackService(db);

        var result = await service.GetByIdAsync(f.Id);

        Assert.NotNull(result);
        Assert.Equal("test", result.Content);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsFalse_WhenNotFound()
    {
        var db = CreateDb();
        var service = new FeedbackService(db);

        var result = await service.UpdateStatusAsync(999, FeedbackStatus.Resolved, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateStatus_SetsStatusAndAdmin()
    {
        var db = CreateDb();
        var user = new User("u", "pw");
        db.Users.Add(user);
        var f = new Feedback { UserId = 1, Type = FeedbackType.ContentFeedback, Content = "test" };
        db.Feedbacks.Add(f);
        await db.SaveChangesAsync();
        var service = new FeedbackService(db);

        var result = await service.UpdateStatusAsync(f.Id, FeedbackStatus.Resolved, 5);

        Assert.True(result);
        var updated = db.Feedbacks.First();
        Assert.Equal(FeedbackStatus.Resolved, updated.Status);
        Assert.Equal(5L, updated.HandledByAdminId);
        Assert.NotNull(updated.HandledAt);
    }
}
