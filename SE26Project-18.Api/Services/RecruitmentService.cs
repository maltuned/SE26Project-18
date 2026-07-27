using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _db;

    public RecruitmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<RecruitmentResponse>> SearchAsync(
        RecruitmentQueryRequest request,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        var query = BaseQuery().Where(r => r.Status == RecruitmentStatus.Open && r.ExpiresAt > now);

        if (request.GameId.HasValue)
            query = query.Where(r => r.Game.Id == request.GameId.Value);

        foreach (var tagId in request.GameTagIds?.Distinct() ?? [])
            query = query.Where(r => r.Game.Tags.Any(t => t.Id == tagId));

        foreach (var tagId in request.RecruitmentTagIds?.Distinct() ?? [])
            query = query.Where(r => r.Tags.Any(t => t.Id == tagId));

        return await ToPagedResponseAsync(query, request.Page, request.PageSize, ct);
    }

    public async Task<PagedResponse<RecruitmentResponse>> GetByRecruiterAsync(
        long recruiterId,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var query = BaseQuery()
            .Where(r => r.Recruiter.Id == recruiterId && r.Status != RecruitmentStatus.Deleted);

        return await ToPagedResponseAsync(query, page, pageSize, ct);
    }

    public async Task<RecruitmentResponse> GetByIdAsync(long id, CancellationToken ct)
    {
        var recruitment =
            await BaseQuery()
                .FirstOrDefaultAsync(r => r.Id == id && r.Status != RecruitmentStatus.Deleted, ct)
            ?? throw new NotFoundException("Recruitment not found.");

        return recruitment.ToResponse();
    }

    public async Task<RecruitmentResponse> CreateAsync(
        long recruiterId,
        CreateRecruitmentRequest request,
        CancellationToken ct
    )
    {
        ValidateTitle(request.Title);
        ValidateExpiry(request.ExpiresAt);

        var game =
            await _db.Games.FindAsync([request.GameId], ct)
            ?? throw new NotFoundException("Game not found.");
        var recruiter =
            await _db.Users.FindAsync([recruiterId], ct)
            ?? throw new NotFoundException("User not found.");
        var tags = await GetTagsAsync(request.RecruitmentTagIds, ct);

        var recruitment = new Recruitment(
            game,
            recruiter,
            request.Title.Trim(),
            request.MaxParticipants,
            request.ExpiresAt
        )
        {
            Description = request.Description,
            Tags = tags,
        };

        _db.Recruitments.Add(recruitment);
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(recruitment.Id, ct);
    }

    public async Task<RecruitmentResponse> UpdateAsync(
        long recruiterId,
        long recruitmentId,
        UpdateRecruitmentRequest request,
        CancellationToken ct
    )
    {
        var recruitment =
            await BaseQuery(tracking: true)
                .FirstOrDefaultAsync(
                    r => r.Id == recruitmentId && r.Status != RecruitmentStatus.Deleted,
                    ct
                )
            ?? throw new NotFoundException("Recruitment not found.");

        if (recruitment.Recruiter.Id != recruiterId)
            throw new ForbiddenException("Only the recruitment recruiter can update it.");

        if (request.Title is not null)
            ValidateTitle(request.Title);
        if (request.ExpiresAt.HasValue)
            ValidateExpiry(request.ExpiresAt.Value);
        var maxParticipants = request.MaxParticipants ?? recruitment.MaxParticipants;
        var expiresAt = request.ExpiresAt ?? recruitment.ExpiresAt;
        var status = request.Status ?? recruitment.Status;

        if (maxParticipants < recruitment.CurrParticipants)
            throw new ValidationException(
                "Maximum participants cannot be less than current participants."
            );

        if (status != RecruitmentStatus.Deleted && recruitment.CurrParticipants >= maxParticipants)
            status = RecruitmentStatus.Closed;

        if (status == RecruitmentStatus.Open && expiresAt <= DateTime.UtcNow)
            throw new ConflictException("An expired recruitment cannot be opened.");

        if (status == RecruitmentStatus.Open && recruitment.CurrParticipants >= maxParticipants)
            throw new ConflictException("A full recruitment cannot be opened.");

        recruitment.Tags = await GetTagsAsync(request.RecruitmentTagIds, ct);

        recruitment.Update(
            request.Title?.Trim() ?? recruitment.Title,
            request.Description ?? recruitment.Description,
            maxParticipants,
            expiresAt,
            status
        );
        await _db.SaveChangesAsync(ct);

        return recruitment.ToResponse();
    }

    private IQueryable<Recruitment> BaseQuery(bool tracking = false)
    {
        var query = _db
            .Recruitments.Include(r => r.Game)
                .ThenInclude(g => g.Tags)
            .Include(r => r.Recruiter)
                .ThenInclude(u => u.Tags)
            .Include(r => r.Tags);

        return tracking ? query : query.AsNoTracking();
    }

    private static async Task<PagedResponse<RecruitmentResponse>> ToPagedResponseAsync(
        IQueryable<Recruitment> query,
        int page,
        int pageSize,
        CancellationToken ct
    )
    {
        var totalCount = await query.CountAsync(ct);
        var recruitments = await query
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<RecruitmentResponse>(
            recruitments.Select(r => r.ToResponse()).ToList(),
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }

    private async Task<List<RecruitmentTag>> GetTagsAsync(
        IReadOnlyCollection<long>? requestedTagIds,
        CancellationToken ct
    )
    {
        var tagIds = requestedTagIds?.Distinct().ToArray() ?? [];
        var tags = await _db.RecruitmentTags.Where(t => tagIds.Contains(t.Id)).ToListAsync(ct);

        if (tags.Count != tagIds.Length)
            throw new NotFoundException("One or more recruitment tags do not exist.");

        return tags;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ValidationException("Recruitment title is required.");
    }

    private static void ValidateExpiry(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new ValidationException("Recruitment expiry must be in the future.");
    }
}
