using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Infrastructure.Embedding;
using SE26Project_18.Backend.Models;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Models.Recommendations;
using SE26Project_18.Backend.Services.Recommendations;

namespace SE26Project_18.Backend.Services;

public class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _db;
    private readonly MapperService _mapper;
    private readonly IRecruitmentRecommendationAlgorithm _recommendation;
    private readonly IEmbeddingSyncScheduler _embeddingSync;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RecruitmentService> _logger;

    public RecruitmentService(
        AppDbContext db,
        MapperService mapper,
        IRecruitmentRecommendationAlgorithm recommendation,
        IEmbeddingSyncScheduler embeddingSync,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RecruitmentService> logger)
    {
        _db = db;
        _mapper = mapper;
        _recommendation = recommendation;
        _embeddingSync = embeddingSync;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
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

    private async Task<List<RecruitmentDetailDto>> ToRankedDtoList(IQueryable<Recruitment> query)
    {
        var ct = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var list = await query.OrderByDescending(recruitment => recruitment.CreatedAt)
            .AsSplitQuery().ToListAsync(ct);
        var userId = GetCurrentUserId();
        if (!userId.HasValue || list.Count == 0)
            return list.Select(_mapper.ToRecruitmentDetailDto).ToList();

        try
        {
            var candidates = list.Select(recruitment =>
                new RecruitmentRecommendationCandidate(recruitment.Id, recruitment.GameId)).ToList();
            var rankedIds = await _recommendation.RankAsync(userId.Value, candidates, ct);
            var order = rankedIds.Select((id, index) => (id, index))
                .ToDictionary(item => item.id, item => item.index);
            return list.OrderBy(item => order.TryGetValue(item.Id, out var index) ? index : int.MaxValue)
                .Select(_mapper.ToRecruitmentDetailDto).ToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception,
                "Recommendation unavailable; using existing recruitment order for user {UserId}",
                userId.Value);
            return list.Select(_mapper.ToRecruitmentDetailDto).ToList();
        }
    }

    private long? GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var value = user?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return long.TryParse(value, out var id) ? id : null;
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

        return await ToRankedDtoList(query);
    }

    public async Task<List<RecruitmentDetailDto>> GetRecruitmentsByGameAsync(long gameId)
    {
        var query = Query().Where(r => r.GameId == gameId && r.Status == RecruitmentStatus.Open);
        return await ToRankedDtoList(query);
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
        await using var transaction = await _db.Database.BeginTransactionAsync();
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
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitment.Id);
        _embeddingSync.Schedule(EmbeddingTarget.User, recruitment.PublisherId);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

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
        var statusChanged = data.TryGetValue("status", out var status);
        if (statusChanged) r.Status = GetStringValue(status!).ToRecruitmentStatus();
        if (data.TryGetValue("expired_at", out var expired)) r.ExpiredAt = DateTime.Parse(GetStringValue(expired));
        if (data.TryGetValue("max_participants", out var max)) r.MaxParticipants = GetIntValue(max);
        if (data.TryGetValue("current_participants", out var curr)) r.CurrentParticipants = GetIntValue(curr);
        var tagsChanged = false;
        if (data.TryGetValue("tags_id", out var tagsId))
        {
            var ids = GetLongArrayValue(tagsId);
            if (ids.Length > 0)
            {
                r.RecruitmentTags = await _db.RecruitmentTags.Where(t => ids.Contains(t.Id)).ToListAsync();
                tagsChanged = true;
            }
        }

        r.UpdatedAt = DateTime.UtcNow;
        if (tagsChanged)
        {
            _embeddingSync.Schedule(EmbeddingTarget.Recruitment, r.Id);
            _embeddingSync.Schedule(EmbeddingTarget.User, await GetAffectedUserIdsAsync(r.Id, r.PublisherId));
        }
        else if (statusChanged)
        {
            _embeddingSync.Schedule(EmbeddingTarget.Recruitment, r.Id);
        }
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
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment, r.Id);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecordViewAsync(long userId, long recruitmentId, CancellationToken ct = default)
    {
        const int maximumAttempts = 5;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var recruitment = await _db.Recruitments.FirstOrDefaultAsync(item =>
                    item.Id == recruitmentId && item.Status != RecruitmentStatus.Deleted, ct);
                if (recruitment == null) return false;
                if (recruitment.PublisherId == userId) return true;

                var user = await _db.Users.FindAsync([userId], ct);
                if (user == null) return false;
                var view = await _db.RecruitmentViews.FirstOrDefaultAsync(item =>
                    item.UserId == userId && item.RecruitmentId == recruitmentId, ct);
                if (view == null)
                {
                    view = new RecruitmentView(user, recruitment);
                    _db.RecruitmentViews.Add(view);
                }
                else
                {
                    view.RecordView();
                }

                if (view.ViewCount <= 3)
                    _embeddingSync.Schedule(EmbeddingTarget.User, userId);

                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }
            catch (DbUpdateException) when (attempt < maximumAttempts)
            {
                await transaction.RollbackAsync(ct);
                _db.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), ct);
            }
        }

        throw new InvalidOperationException("无法并发记录招募浏览。");
    }

    private async Task<IReadOnlyCollection<long>> GetAffectedUserIdsAsync(long recruitmentId, long publisherId)
    {
        var responders = _db.Responses.Where(response => response.RecruitmentId == recruitmentId)
            .Select(response => response.ResponserId);
        var viewers = _db.RecruitmentViews.Where(view => view.RecruitmentId == recruitmentId)
            .Select(view => view.UserId);
        var users = await responders.Concat(viewers).Distinct().ToListAsync();
        users.Add(publisherId);
        return users;
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
