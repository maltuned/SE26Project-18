using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class ChatMappings
{
    public static ChatResponse ToResponse(this Chat chat)
    {
        var lastMessage = chat
            .Messages.OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .FirstOrDefault();

        return chat.ToResponse(
            lastMessage is null
                ? null
                : new MessageResponse(
                    lastMessage.Id,
                    lastMessage.Sender.Id,
                    lastMessage.Content,
                    lastMessage.SentAt
                )
        );
    }

    public static ChatResponse ToResponse(this Chat chat, MessageResponse? lastMessage)
    {
        return new ChatResponse(
            chat.Id,
            chat.Recruitment.Id,
            chat.User1.Id,
            chat.User2.Id,
            chat.Status,
            chat.NewMsgsCntForUser1,
            chat.NewMsgsCntForUser2,
            lastMessage
        );
    }
}
