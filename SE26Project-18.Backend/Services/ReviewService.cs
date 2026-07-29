using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public sealed class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Review> CreateAsync(long reviewerId, long revieweeId, string content)
    {
        if (reviewerId == revieweeId)
            throw new ArgumentException("不能评价自己");

        var existing = await _db.Reviews
            .FirstOrDefaultAsync(r => r.ReviewerId == reviewerId && r.RevieweeId == revieweeId);

        if (existing != null)
            throw new ArgumentException("您已经评价过该用户");

        var review = new Review
        {
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Content = content,
            Status = ReviewStatus.Visible,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return review;
    }

    public async Task<List<ReviewDto>> GetReviewsForUserAsync(long userId)
    {
        return await _db.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.RevieweeId == userId && r.Status == ReviewStatus.Visible)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ReviewerId = r.ReviewerId,
                ReviewerNickname = r.Reviewer.Nickname ?? r.Reviewer.Username,
                ReviewerAvatar = r.Reviewer.Avatar ?? "",
                RevieweeId = r.RevieweeId,
                Content = r.Content,
                Status = "显示",
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd"),
            })
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(long reviewId, ReviewStatus status)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return false;

        review.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasReviewedAsync(long reviewerId, long revieweeId)
    {
        return await _db.Reviews
            .AnyAsync(r => r.ReviewerId == reviewerId && r.RevieweeId == revieweeId);
    }

    public async Task<string?> GetReviewContentAsync(long reviewId)
    {
        return await _db.Reviews
            .Where(r => r.Id == reviewId)
            .Select(r => r.Content)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ReviewDto>> GetAllAsync()
    {
        return await _db.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Reviewee)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ReviewerId = r.ReviewerId,
                ReviewerNickname = r.Reviewer.Nickname ?? r.Reviewer.Username,
                ReviewerAvatar = r.Reviewer.Avatar ?? "",
                RevieweeId = r.RevieweeId,
                RevieweeNickname = r.Reviewee.Nickname ?? r.Reviewee.Username,
                Content = r.Content,
                Status = r.Status == ReviewStatus.Visible ? "显示" : "隐藏",
                CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd"),
            })
            .ToListAsync();
    }
}