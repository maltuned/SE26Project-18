using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Dtos.Response;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Services;

public class ResponseService
{
    private readonly AppDbContext _db;

    public ResponseService(AppDbContext db)
    {
        _db = db;
    }

    // 回应招募
    public async Task<ResponseDto> CreateAsync(long userId, CreateResponseDto dto)
    {
        var recruitment = await _db.Recruitments
            .Include(r => r.Game)
            .FirstOrDefaultAsync(r => r.Id == dto.RecruitmentId)
            ?? throw new InvalidOperationException("招募不存在");

        if (recruitment.Status != RecruitmentStatus.Open)
            throw new InvalidOperationException("招募已关闭");

        if (recruitment.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("招募已过期");

        if (recruitment.CurrParticipants >= recruitment.MaxParticipants)
            throw new InvalidOperationException("招募已满员");

        var responder = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("用户不存在");

        // 检查是否已回应过该招募（Pending/Rejected/Accepted）
        var existing = await _db.Responses
            .FirstOrDefaultAsync(r =>
                r.Recruitment.Id == dto.RecruitmentId &&
                r.Responder.Id == userId);
        if (existing != null)
        {
            var msg = existing.Status switch
            {
                ResponseType.Pending => "已有待审批的回应，请勿重复提交",
                ResponseType.Accepted => "该招募已接受你的回应",
                ResponseType.Rejected => "该招募已拒绝你的回应，无法再次申请",
                _ => "已回应过该招募"
            };
            throw new InvalidOperationException(msg);
        }

        // 招募发布者信息：暂时无法从Recruitment直接获取，留待后续完善
        var recruiter = await _db.Users.FirstAsync();

        var response = new Response(recruitment, responder, recruiter, dto.GreetingMessage);
        _db.Responses.Add(response);
        await _db.SaveChangesAsync();

        return MapToDto(response);
    }

    // 单条回应详情
    public async Task<ResponseDto> GetByIdAsync(long responseId, long userId)
    {
        var response = await _db.Responses
            .Include(r => r.Recruitment)
            .Include(r => r.Responder)
            .Include(r => r.Recruiter)
            .FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new InvalidOperationException("回应不存在");

        if (response.Responder.Id != userId && response.Recruiter.Id != userId)
            throw new InvalidOperationException("无权查看该回应");

        return MapToDto(response);
    }

    // 我收到的回应列表（招募发布者视角）
    public async Task<PagedResult<ResponseDto>> GetInboxAsync(
        long recruiterId,
        long? recruitmentId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = BuildBaseQuery()
            .Where(r => r.Recruiter.Id == recruiterId);

        if (recruitmentId.HasValue)
            query = query.Where(r => r.Recruitment.Id == recruitmentId.Value);

        return await PaginateAsync(query, page, pageSize);
    }

    // 我发出的回应列表（回应者视角）
    public async Task<PagedResult<ResponseDto>> GetOutboxAsync(
        long responderId, int page = 1, int pageSize = 20)
    {
        var query = BuildBaseQuery()
            .Where(r => r.Responder.Id == responderId);

        return await PaginateAsync(query, page, pageSize);
    }

    // 构建带 Include 的基础查询
    private IQueryable<Models.Entities.Response> BuildBaseQuery()
    {
        return _db.Responses
            .Include(r => r.Recruitment)
            .Include(r => r.Responder)
            .Include(r => r.Recruiter);
    }

    // 通用分页
    private async Task<PagedResult<ResponseDto>> PaginateAsync(
        IQueryable<Models.Entities.Response> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var responses = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = responses.Select(MapToDto).ToList();
        return new PagedResult<ResponseDto>(items, totalCount, page, pageSize);
    }

    // 撤回回应（回应者取消自己的Pending回应）
    public async Task CancelAsync(long responseId, long responderId)
    {
        var response = await _db.Responses
            .Include(r => r.Responder)
            .FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new InvalidOperationException("回应不存在");

        if (response.Responder.Id != responderId)
            throw new InvalidOperationException("只能撤回自己的回应");

        if (response.Status != ResponseType.Pending)
            throw new InvalidOperationException("只有待审批的回应才能撤回");

        _db.Responses.Remove(response);
        await _db.SaveChangesAsync();
    }

    // 接受回应
    public async Task AcceptAsync(long responseId, long recruiterId)
    {
        var response = await _db.Responses
            .Include(r => r.Recruitment)
            .Include(r => r.Responder)
            .Include(r => r.Recruiter)
            .FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new InvalidOperationException("回应不存在");

        if (response.Recruiter.Id != recruiterId)
            throw new InvalidOperationException("只有招募发布者才能审批回应");

        if (response.Status != ResponseType.Pending)
            throw new InvalidOperationException("该回应已被处理");

        if (response.Recruitment.Status != RecruitmentStatus.Open)
            throw new InvalidOperationException("招募已关闭，无法接受回应");

        response.Accept();
        response.Recruitment.AddParticipant();

        var chat = new Chat(response.Recruitment, response.Recruiter, response.Responder);
        _db.Chats.Add(chat);
        response.SetChat(chat);

        await SaveWithConcurrencyCheck();
    }

    // 拒绝回应
    public async Task RejectAsync(long responseId, long recruiterId)
    {
        var response = await _db.Responses
            .Include(r => r.Recruiter)
            .FirstOrDefaultAsync(r => r.Id == responseId)
            ?? throw new InvalidOperationException("回应不存在");

        if (response.Recruiter.Id != recruiterId)
            throw new InvalidOperationException("只有招募发布者才能审批回应");

        if (response.Status != ResponseType.Pending)
            throw new InvalidOperationException("该回应已被处理");

        response.Reject();

        // TODO: 触发画像更新事件（算法视图要求）

        await SaveWithConcurrencyCheck();
    }

    // 保存并处理并发冲突
    private async Task SaveWithConcurrencyCheck()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("该回应已被其他人修改，请刷新后重试");
        }
    }

    // 映射：Response 实体 → ResponseDto
    private static ResponseDto MapToDto(Models.Entities.Response r)
    {
        return new ResponseDto
        {
            Id = r.Id,
            RecruitmentId = r.Recruitment.Id,
            RecruitmentTitle = r.Recruitment.Title,
            ResponderId = r.Responder.Id,
            ResponderName = r.Responder.Nickname,
            RecruiterId = r.Recruiter.Id,
            RecruiterName = r.Recruiter.Nickname,
            ChatId = r.ChatId,
            GreetingMessage = r.GreetingMessage,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        };
    }
}
