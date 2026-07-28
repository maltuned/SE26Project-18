namespace SE26Project_18.Api.Models.Requests;

public sealed record SendMessageRequest(long ChatId, long ReceiverId, string Content);
