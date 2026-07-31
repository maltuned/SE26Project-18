using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IFeedbackService
{
    Task SubmitFeedbackAsync(long userId, FeedbackType type, string content);
    Task<List<Feedback>> GetAllAsync(FeedbackStatus? status = null);
    Task<Feedback?> GetByIdAsync(long id);
    Task<bool> UpdateStatusAsync(long id, FeedbackStatus status, long adminId);
}