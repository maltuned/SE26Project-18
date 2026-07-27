using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IFeedbackService
{
    Task SubmitFeedbackAsync(long userId, FeedbackType type, string content);
}