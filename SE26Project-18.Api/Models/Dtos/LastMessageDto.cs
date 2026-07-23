namespace SE26Project_18.Api.Models.Dtos;

public sealed record LastMessageDto(long SenderId, string Content, DateTime SentAt);

