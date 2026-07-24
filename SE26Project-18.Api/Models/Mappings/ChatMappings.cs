using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

public static class ChatMappings
{
    public static ChatResponse ToResponse(this Chat chat)
    {
        var lastMessage = chat
            .Messages.OrderByDescending(message => message.SentAt)
            .FirstOrDefault();

        return new ChatResponse(
            chat.Id,
            chat.Recruitment.Id,
            chat.User1.Id,
            chat.User2.Id,
            chat.Status,
            chat.NewMsgsCntForUser1,
            chat.NewMsgsCntForUser2,
            lastMessage is null
                ? null
                : new MessageResponse(
                    lastMessage.Sender.Id,
                    lastMessage.Content,
                    lastMessage.SentAt
                )
        );
    }
}
