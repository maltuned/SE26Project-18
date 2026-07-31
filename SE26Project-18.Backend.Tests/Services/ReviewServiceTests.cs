using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ReviewServiceTests
{
    [Fact]
    public async Task Create_Throws_WhenSelfReview()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        var service = new ReviewService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(user.Id, user.Id, "Great!"));
    }

    [Fact]
    public async Task Create_Throws_WhenDuplicateReview()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("reviewer", "pw"));
        db.Users.Add(new User("reviewee", "pw"));
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "Old review" });
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(1, 2, "Another review"));
    }

    [Fact]
    public async Task Create_Succeeds_WithValidData()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var review = await service.CreateAsync(1, 2, "Excellent player!");

        Assert.NotNull(review);
        Assert.Equal(1L, review.ReviewerId);
        Assert.Equal(2L, review.RevieweeId);
        Assert.Equal("Excellent player!", review.Content);
        Assert.Equal(ReviewStatus.Visible, review.Status);
    }

    [Fact]
    public async Task GetReviewsForUser_ReturnsVisibleReviews()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        db.Users.Add(new User("r3", "pw"));
        await db.SaveChangesAsync();
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "Good", Status = ReviewStatus.Visible });
        db.Reviews.Add(new Review { ReviewerId = 3, RevieweeId = 2, Content = "Hidden", Status = ReviewStatus.Hidden });
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var result = await service.GetReviewsForUserAsync(2);

        Assert.Single(result);
        Assert.Equal("Good", result[0].Content);
    }

    [Fact]
    public async Task HasReviewed_ReturnsTrue_WhenReviewExists()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "x" });
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var result = await service.HasReviewedAsync(1, 2);

        Assert.True(result);
    }

    [Fact]
    public async Task HasReviewed_ReturnsFalse_WhenNoReview()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var result = await service.HasReviewedAsync(1, 2);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsFalse_WhenNotFound()
    {
        var db = TestDbContextFactory.Create();
        var service = new ReviewService(db);

        var result = await service.UpdateStatusAsync(999, ReviewStatus.Hidden);

        Assert.False(result);
    }

    [Fact]
    public async Task GetReviewContent_ReturnsContent()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "My review content" });
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var content = await service.GetReviewContentAsync(db.Reviews.First().Id);

        Assert.Equal("My review content", content);
    }

    [Fact]
    public async Task GetReviewContent_ReturnsNull_WhenNotFound()
    {
        var db = TestDbContextFactory.Create();
        var service = new ReviewService(db);

        var content = await service.GetReviewContentAsync(999);

        Assert.Null(content);
    }

    [Fact]
    public async Task GetAll_ReturnsAllReviews()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("r1", "pw"));
        db.Users.Add(new User("r2", "pw"));
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "A", Status = ReviewStatus.Visible });
        db.Reviews.Add(new Review { ReviewerId = 1, RevieweeId = 2, Content = "B", Status = ReviewStatus.Hidden });
        await db.SaveChangesAsync();
        var service = new ReviewService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }
}
