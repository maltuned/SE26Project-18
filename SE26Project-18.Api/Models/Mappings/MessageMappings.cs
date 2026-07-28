using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Models.Mappings;

internal static class MessageMappings
{
    public static MessageResponse ToResponse(this Message message)
    {
        return new MessageResponse(
            message.Id,
            message.Chat.Id,
            message.Sender.Id,
            message.Receiver.Id,
            message.Content,
            message.SentAt,
            message.Sender.ToResponse(),
            message.Receiver.ToResponse()
        );
    }
}
