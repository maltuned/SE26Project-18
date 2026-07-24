using SE26Project_18.Backend.Models.Dtos;

namespace SE26Project_18.Backend.Services;

public interface IMessageService
{
    Task<List<MessageDto>> GetMessagesByChatAsync(long chatId);
    Task<MessageDto> SendMessageAsync(long chatId, long senderId, long receiverId, string content);
}
