namespace SE26Project_18.Api.Models.Responses;

public sealed record MessageResponse(long Id, long SenderId, string Content, DateTime SentAt);
