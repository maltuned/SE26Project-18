using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Dtos.Recruitment;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Entities;

namespace SE26Project_18.Api.Services;

internal sealed class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _db;

    public RecruitmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RecruitmentListResponse>> GetListAsync(
        string? gameName,
        List<long>? gameTagIds,
        List<long>? recruitmentTagIds,
        CancellationToken ct)
    {
        var query = _db.Recruitments
            .Include(r => r.Game).ThenInclude(g => g.Tags)
            .Include(r => r.Recruiter)
            .Include(r => r.Responses).ThenInclude(r => r.Responder)
            .Where(r => r.Status != RecruitmentStatus.Deleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(gameName))
            query = query.Where(r => r.Game.Name.Contains(gameName));

        if (gameTagIds is { Count: > 0 })
            query = query.Where(r => r.Game.Tags.Any(t => gameTagIds.Contains(t.Id)));

        var list = await query.OrderByDescending(r => r.ExpiresAt).ToListAsync(ct);
        return list.Select(MapToListResponse).ToList();
    }

    public async Task<RecruitmentDetailResponse> GetByIdAsync(long id, CancellationToken ct)
    {
        var r = await BaseDetailQuery().FirstOrDefaultAsync(r => r.Id == id, ct)
                ?? throw new NotFoundException("Recruitment not found.");
        return MapToDetailResponse(r);
    }

    public async Task<RecruitmentDetailResponse> CreateAsync(
        long userId, CreateRecruitmentRequest req, CancellationToken ct)
    {
        var game = await _db.Games.FindAsync([req.GameId], ct)
                   ?? throw new NotFoundException("Game not found.");
        var user = await _db.Users.FindAsync([userId], ct)
                   ?? throw new NotFoundException("User not found.");

        var recruitment = new Recruitment(game, user, req.Title,
            req.MaxParticipants, DateTime.UtcNow.AddHours(req.DurationHours))
        {
            Description = req.Description
        };
        _db.Recruitments.Add(recruitment);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(recruitment.Id, ct);
    }

    public async Task<RecruitmentDetailResponse> UpdateAsync(
        long id, long userId, UpdateRecruitmentRequest req, CancellationToken ct)
    {
        var recruitment = await _db.Recruitments
                              .Include(r => r.Recruiter)
                              .FirstOrDefaultAsync(r => r.Id == id, ct)
                          ?? throw new NotFoundException("Recruitment not found.");

        if (recruitment.Recruiter.Id != userId)
            throw new ForbiddenException("Only the recruiter can update this recruitment.");

        if (req.Title != null) recruitment.Title = req.Title;
        if (req.Description != null) recruitment.Description = req.Description;
        if (req.MaxParticipants.HasValue) recruitment.MaxParticipants = req.MaxParticipants.Value;

        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(long id, long userId, CancellationToken ct)
    {
        var recruitment = await _db.Recruitments
                              .Include(r => r.Recruiter)
                              .FirstOrDefaultAsync(r => r.Id == id, ct)
                          ?? throw new NotFoundException("Recruitment not found.");

        if (recruitment.Recruiter.Id != userId)
            throw new ForbiddenException("Only the recruiter can delete this recruitment.");

        recruitment.Status = RecruitmentStatus.Deleted;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<RecruitmentListResponse>> GetByPublisherIdAsync(long userId, CancellationToken ct)
    {
        var list = await BaseDetailQuery()
            .Where(r => r.Recruiter.Id == userId && r.Status != RecruitmentStatus.Deleted)
            .OrderByDescending(r => r.ExpiresAt)
            .ToListAsync(ct);
        return list.Select(MapToListResponse).ToList();
    }

    public async Task<RecruitmentListResponse?> GetByChatIdAsync(long chatId, CancellationToken ct)
    {
        var chat = await _db.Chats.Include(c => c.Recruitment).ThenInclude(r => r.Game)
                       .Include(c => c.Recruitment).ThenInclude(r => r.Recruiter)
                       .FirstOrDefaultAsync(c => c.Id == chatId, ct);
        if (chat?.Recruitment == null) return null;
        return MapToListResponse(chat.Recruitment);
    }

    public async Task<List<RecruitmentListResponse>> GetByGameIdAsync(long gameId, CancellationToken ct)
    {
        var list = await BaseDetailQuery()
            .Where(r => r.Game.Id == gameId && r.Status != RecruitmentStatus.Deleted)
            .OrderByDescending(r => r.ExpiresAt)
            .ToListAsync(ct);
        return list.Select(MapToListResponse).ToList();
    }

    private IQueryable<Recruitment> BaseDetailQuery()
    {
        return _db.Recruitments
            .Include(r => r.Game).ThenInclude(g => g.Tags)
            .Include(r => r.Recruiter)
            .Include(r => r.Responses).ThenInclude(r => r.Responder);
    }

    private static RecruitmentListResponse MapToListResponse(Recruitment r)
    {
        return new RecruitmentListResponse
        {
            Id = r.Id,
            GameId = r.Game.Id,
            GameName = r.Game.Name,
            Title = r.Title,
            Description = r.Description,
            MaxParticipants = r.MaxParticipants,
            CurrParticipants = r.CurrParticipants,
            Status = r.Status.ToString(),
            ExpiresAt = r.ExpiresAt,
            RecruiterId = r.Recruiter.Id,
            RecruiterName = r.Recruiter.Nickname,
            GameTags = r.Game.Tags.Select(t => new TagInfo { Id = t.Id, Name = t.Name }).ToList(),
            RecruitmentTags = [],
        };
    }

    private static RecruitmentDetailResponse MapToDetailResponse(Recruitment r)
    {
        return new RecruitmentDetailResponse
        {
            Id = r.Id,
            GameId = r.Game.Id,
            GameName = r.Game.Name,
            Title = r.Title,
            Description = r.Description,
            MaxParticipants = r.MaxParticipants,
            CurrParticipants = r.CurrParticipants,
            Status = r.Status.ToString(),
            ExpiresAt = r.ExpiresAt,
            RecruiterId = r.Recruiter.Id,
            RecruiterName = r.Recruiter.Nickname,
            GameTags = r.Game.Tags.Select(t => new TagInfo { Id = t.Id, Name = t.Name }).ToList(),
            RecruitmentTags = [],
            Responses = r.Responses.Select(resp => new RecruiterResponseInfo
            {
                Id = resp.Id,
                ResponderId = resp.Responder.Id,
                ResponderName = resp.Responder.Nickname,
                Status = resp.Type.ToString(),
            }).ToList(),
        };
    }
}
