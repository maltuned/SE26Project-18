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
}