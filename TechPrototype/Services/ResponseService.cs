using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public class ResponseService : IResponseService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public ResponseService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    private IQueryable<Models.Entities.Response> Query()
    {
        return _db.Responses
            .Include(r => r.Responser)
            .Include(r => r.Recruitment);
    }

    public async Task<List<ResponseDto>> GetResponsesByRecruitmentAsync(long recruitmentId)
    {
        var responses = await Query()
            .Where(r => r.RecruitmentId == recruitmentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return responses.Select(_mapper.ToResponseDto).ToList();
    }

    public async Task<List<ResponseDto>> GetResponsesByUserAsync(long userId)
    {
        var responses = await Query()
            .Where(r => r.ResponserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return responses.Select(_mapper.ToResponseDto).ToList();
    }

    public async Task<ResponseDto> CreateResponseAsync(long recruitmentId, long responserId)
    {
        // Check if response already exists
        var existing = await _db.Responses.AnyAsync(r =>
            r.RecruitmentId == recruitmentId && r.ResponserId == responserId);
        if (existing)
            throw new InvalidOperationException("已回应过该招募");

        var recruitment = await _db.Recruitments.FindAsync(recruitmentId)
            ?? throw new KeyNotFoundException("招募不存在");
        var responser = await _db.Users.FindAsync(responserId)
            ?? throw new KeyNotFoundException("用户不存在");

        var response = new Models.Entities.Response
        {
            RecruitmentId = recruitmentId,
            ResponserId = responserId,
            ResponseStatus = ResponseStatus.Responded,
            Recruitment = recruitment,
            Responser = responser,
        };

        _db.Responses.Add(response);
        await _db.SaveChangesAsync();
        return _mapper.ToResponseDto(response);
    }

    public async Task<bool> DeleteResponseAsync(long id, string reason)
    {
        var response = await _db.Responses
            .Include(r => r.Recruitment)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (response == null) return false;

        response.ResponseStatus = ResponseStatus.Deleted;
        response.UpdatedAt = DateTime.UtcNow;

        var publisherId = response.Recruitment.PublisherId;
        var responserId = response.ResponserId;

        // Find or create chat between publisher and responser
        var chat = await _db.Chats
            .FirstOrDefaultAsync(c =>
                (c.RecruiterId == publisherId && c.ResponserId == responserId) ||
                (c.RecruiterId == responserId && c.ResponserId == publisherId));

        if (chat == null)
        {
            chat = new Chat
            {
                RecruitmentId = response.RecruitmentId,
                RecruiterId = publisherId,
                ResponserId = responserId,
                ChatStatus = ChatStatus.Restricted,
                NewMessageAt = DateTime.UtcNow,
            };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync(); // Save to get chat.Id
        }

        // Send rejection message from publisher to responser
        var message = new Message($"回应已拒绝（原因：{reason}）")
        {
            ChatId = chat.Id,
            SenderId = publisherId,
            ReceiverId = responserId,
        };
        _db.Messages.Add(message);

        chat.NewMessageAt = DateTime.UtcNow;
        chat.UpdatedAt = DateTime.UtcNow;
        chat.ChatStatus = ChatStatus.Restricted;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ResponseDto?> UpdateResponseStatusAsync(long id, string responseStatus)
    {
        var response = await Query().FirstOrDefaultAsync(r => r.Id == id);
        if (response == null) return null;
        response.ResponseStatus = responseStatus.ToResponseStatus();
        response.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return _mapper.ToResponseDto(response);
    }
}
