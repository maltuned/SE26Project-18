using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IReviewService
{
    Task<Review> CreateAsync(long reviewerId, long revieweeId, string content);
    Task<List<ReviewDto>> GetReviewsForUserAsync(long userId);
    Task<List<ReviewDto>> GetAllAsync();
    Task<bool> UpdateStatusAsync(long reviewId, ReviewStatus status);
    Task<bool> HasReviewedAsync(long reviewerId, long revieweeId);
    Task<string?> GetReviewContentAsync(long reviewId);
}