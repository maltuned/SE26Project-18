namespace SE26Project_18.Api.Models.Responses;

public sealed record MessageResponse(
    long Id,
    long ChatId,
    long SenderId,
    long ReceiverId,
    string Content,
    DateTime SentAt,
    UserResponse Sender,
    UserResponse Receiver
);
