using System.Text.Json.Serialization;

namespace SE26Project_18.Backend.Models.Dtos;

public class CreateReviewDto
{
    [JsonPropertyName("reviewee_id")]
    public long RevieweeId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ReviewDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("reviewer_id")]
    public long ReviewerId { get; set; }

    [JsonPropertyName("reviewer_nickname")]
    public string ReviewerNickname { get; set; } = string.Empty;

    [JsonPropertyName("reviewer_avatar")]
    public string ReviewerAvatar { get; set; } = string.Empty;

    [JsonPropertyName("reviewee_id")]
    public long RevieweeId { get; set; }

    [JsonPropertyName("reviewee_nickname")]
    public string RevieweeNickname { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}