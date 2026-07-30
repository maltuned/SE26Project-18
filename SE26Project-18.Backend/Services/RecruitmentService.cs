using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;

    public RecruitmentService(AppDbContext db, MapperService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    private IQueryable<Recruitment> Query()
    {
        return _db.Recruitments
            .Include(r => r.Publisher)
            .Include(r => r.Game!).ThenInclude(g => g.Tags)
            .Include(r => r.GameTags)
            .Include(r => r.RecruitmentTags);
    }

    private async Task<List<RecruitmentDetailDto>> ToDtoList(IQueryable<Recruitment> query)
    {
        var list = await query.ToListAsync();
        return list.Select(_mapper.ToRecruitmentDetailDto).ToList();
    }

    public async Task<List<RecruitmentDetailDto>> GetRecruitmentsAsync(
        string gameName = "", long[]? gameTags = null, long[]? recruitmentTags = null)
    {
        var query = Query().Where(r => r.Status == RecruitmentStatus.Open);

        if (!string.IsNullOrEmpty(gameName))
            query = query.Where(r => (r.Game != null && r.Game.Name.Contains(gameName)) || r.GameName.Contains(gameName));

        if (gameTags is { Length: > 0 })
            query = query.Where(r => r.GameTags.Any(gt => gameTags.Contains(gt.Id)));

        if (recruitmentTags is { Length: > 0 })
            query = query.Where(r => r.RecruitmentTags.Any(rt => recruitmentTags.Contains(rt.Id)));

        query = query.OrderByDescending(r => r.CreatedAt);
        return await ToDtoList(query);
    }

    public async Task<List<RecruitmentDetailDto>> GetRecruitmentsByGameAsync(long gameId)
    {
        var query = Query().Where(r => r.GameId == gameId && r.Status == RecruitmentStatus.Open)
            .OrderByDescending(r => r.CreatedAt);
        return await ToDtoList(query);
    }

    public async Task<RecruitmentDetailDto?> GetRecruitmentByIdAsync(long id)
    {
        var r = await Query().FirstOrDefaultAsync(r => r.Id == id);
        return r == null ? null : _mapper.ToRecruitmentDetailDto(r);
    }

    public async Task<List<RecruitmentDetailDto>> GetRecruitmentsByPublisherIdAsync(long publisherId)
    {
        var query = Query().Where(r => r.PublisherId == publisherId && r.Status != RecruitmentStatus.Deleted)
            .OrderByDescending(r => r.CreatedAt);
        return await ToDtoList(query);
    }

    public async Task<RecruitmentDetailDto?> GetRecruitmentByChatIdAsync(long chatId)
    {
        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return null;
        return await GetRecruitmentByIdAsync(chat.RecruitmentId);
    }

    public async Task<RecruitmentDetailDto> CreateRecruitmentAsync(RecruitmentDto dto)
    {
        if (dto.GameId == null)
            throw new ArgumentException("游戏ID不能为空");
        var game = await _db.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == dto.GameId.Value)
            ?? throw new KeyNotFoundException("游戏不存在");
        var publisher = await _db.Users.FindAsync(dto.PublisherId)
            ?? throw new KeyNotFoundException("用户不存在");

        var recruitment = new Recruitment(dto.Title, DateTime.Parse(dto.ExpiredAt), dto.MaxParticipants)
        {
            PublisherId = dto.PublisherId,
            GameId = dto.GameId.Value,
            Description = dto.Description,
            Status = dto.Status.ToRecruitmentStatus(),
            CurrentParticipants = dto.CurrentParticipants,
            Publisher = publisher,
            Game = game,
            GameTags = [.. game.Tags],
        };

        if (dto.TagsId is { Length: > 0 })
        {
            recruitment.RecruitmentTags = await _db.RecruitmentTags
                .Where(t => dto.TagsId.Contains(t.Id)).ToListAsync();
        }

        _db.Recruitments.Add(recruitment);
        await _db.SaveChangesAsync();

        // Re-query to get full navigation graph
        return (await GetRecruitmentByIdAsync(recruitment.Id))!;
    }

    public async Task<RecruitmentDetailDto?> UpdateRecruitmentAsync(long id, Dictionary<string, object> data)
    {
        var r = await Query().FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) return null;

        if (r.Status == RecruitmentStatus.Deleted)
            return _mapper.ToRecruitmentDetailDto(r);

        if (data.TryGetValue("title", out var title)) r.Title = GetStringValue(title);
        if (data.TryGetValue("description", out var desc)) r.Description = GetStringValue(desc);
        if (data.TryGetValue("status", out var status)) r.Status = GetStringValue(status).ToRecruitmentStatus();
        if (data.TryGetValue("expired_at", out var expired)) r.ExpiredAt = DateTime.Parse(GetStringValue(expired));
        if (data.TryGetValue("max_participants", out var max)) r.MaxParticipants = GetIntValue(max);
        if (data.TryGetValue("current_participants", out var curr)) r.CurrentParticipants = GetIntValue(curr);
        if (data.TryGetValue("tags_id", out var tagsId))
        {
            var ids = GetLongArrayValue(tagsId);
            if (ids.Length > 0)
            {
                r.RecruitmentTags = await _db.RecruitmentTags.Where(t => ids.Contains(t.Id)).ToListAsync();
            }
        }

        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return _mapper.ToRecruitmentDetailDto(r);
    }

    private static string GetStringValue(object value)
    {
        if (value is System.Text.Json.JsonElement elem)
            return elem.GetString() ?? string.Empty;
        return value?.ToString() ?? string.Empty;
    }

    private static int GetIntValue(object value)
    {
        if (value is System.Text.Json.JsonElement elem)
            return elem.GetInt32();
        return Convert.ToInt32(value);
    }

    private static long[] GetLongArrayValue(object value)
    {
        if (value is System.Text.Json.JsonElement elem && elem.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            return elem.EnumerateArray().Select(e => e.GetInt64()).ToArray();
        }
        if (value is long[] longs) return longs;
        return [];
    }

    public async Task<bool> DeleteRecruitmentAsync(long id)
    {
        var r = await _db.Recruitments.FindAsync(id);
        if (r == null) return false;
        r.Status = RecruitmentStatus.Deleted;
        r.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RecruitmentDetailDto>> SearchRecruitmentsAsync(string query)
    {
        if (string.IsNullOrEmpty(query))
            return await ToDtoList(Query().OrderByDescending(r => r.CreatedAt));

        if (long.TryParse(query, out var id))
            return await ToDtoList(Query().Where(r => r.Id == id));

        return await ToDtoList(Query()
            .Where(r => r.Title.Contains(query))
            .OrderByDescending(r => r.CreatedAt));
    }
}