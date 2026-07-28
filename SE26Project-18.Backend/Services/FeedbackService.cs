using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public sealed class FeedbackService : IFeedbackService
{
    private readonly AppDbContext _db;

    public FeedbackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SubmitFeedbackAsync(long userId, FeedbackType type, string content)
    {
        var feedback = new Feedback
        {
            UserId = userId,
            Type = type,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Feedback>> GetAllAsync(FeedbackStatus? status = null)
    {
        var query = _db.Feedbacks.Include(f => f.User).AsQueryable();

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(long id, FeedbackStatus status, long adminId)
    {
        var feedback = await _db.Feedbacks.FindAsync(id);
        if (feedback == null) return false;

        feedback.Status = status;
        feedback.HandledAt = DateTime.UtcNow;
        feedback.HandledByAdminId = adminId;
        await _db.SaveChangesAsync();
        return true;
    }
}
