using System.Data;
using Microsoft.EntityFrameworkCore;
using Milvus.Client;
using MySqlConnector;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Recommendations;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Services;

internal sealed class RecruitmentService : IRecruitmentService
{
    private readonly AppDbContext _db;

    private readonly IRecruitmentRecommendationAlgorithm _recommendationAlgorithm;

    private readonly IEmbeddingSyncScheduler _embeddingSync;

    private readonly ILogger<RecruitmentService> _logger;

    public RecruitmentService(
        AppDbContext db,
        IRecruitmentRecommendationAlgorithm recommendationAlgorithm,
        IEmbeddingSyncScheduler embeddingSync,
        ILogger<RecruitmentService> logger
    )
    {
        _db = db;
        _recommendationAlgorithm = recommendationAlgorithm;
        _embeddingSync = embeddingSync;
        _logger = logger;
    }

    public async Task<PagedResponse<RecruitmentResponse>> SearchAsync(
        long userId,
        RecruitmentQueryRequest request,
        CancellationToken ct
    )
    {
        var now = DateTime.UtcNow;
        var query = ApplySearchFilters(_db.Recruitments.AsNoTracking(), userId, request, now);

        var candidates = await query
            .Select(recruitment => new RecruitmentRecommendationCandidate(
                recruitment.Id,
                recruitment.Recruiter.Id,
                recruitment.Game.Id
            ))
            .ToListAsync(ct);
        if (candidates.Count == 0)
        {
            return new PagedResponse<RecruitmentResponse>([], request.Page, request.PageSize, 0, 0);
        }

        IReadOnlyList<long> rankedIds;
        try
        {
            rankedIds = await _recommendationAlgorithm.RankAsync(userId, candidates, ct);
        }
        catch (Exception exception) when (
            exception is ServiceUnavailableException or HttpRequestException or MilvusException
        )
        {
            _logger.LogWarning(
                exception,
                "Recommendation dependencies are unavailable; using newest-first fallback for user {UserId}",
                userId
            );
            rankedIds = candidates
                .OrderByDescending(candidate => candidate.Id)
                .Select(candidate => candidate.Id)
                .ToList();
        }
        var eligibleIds = new HashSet<long>();
        foreach (var idBatch in rankedIds.Chunk(1_000))
        {
            var batchIds = await ApplySearchFilters(
                    _db.Recruitments.AsNoTracking(),
                    userId,
                    request,
                    DateTime.UtcNow
                )
                .Where(recruitment => idBatch.Contains(recruitment.Id))
                .Select(recruitment => recruitment.Id)
                .ToListAsync(ct);
            eligibleIds.UnionWith(batchIds);
        }
        rankedIds = rankedIds.Where(eligibleIds.Contains).ToList();
        var totalCount = rankedIds.Count;
        var responses = new List<RecruitmentResponse>(request.PageSize);
        var offset = (request.Page - 1) * request.PageSize;
        while (responses.Count < request.PageSize && offset < rankedIds.Count)
        {
            var pageIds = rankedIds.Skip(offset).Take(request.PageSize - responses.Count).ToList();
            offset += pageIds.Count;
            var pageOrder = pageIds
                .Select((id, index) => (id, index))
                .ToDictionary(item => item.id, item => item.index);
            var items = await ApplySearchFilters(BaseQuery(), userId, request, DateTime.UtcNow)
                .Where(recruitment => pageIds.Contains(recruitment.Id))
                .AsSplitQuery()
                .ToListAsync(ct);
            responses.AddRange(
                items
                    .OrderBy(recruitment => pageOrder[recruitment.Id])
                    .Select(recruitment => recruitment.ToResponse())
            );
        }

        return new PagedResponse<RecruitmentResponse>(
            responses,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)
        );
    }

    public async Task<PagedResponse<RecruitmentResponse>> GetByRecruiterAsync(
        long recruiterId,
        int page,
        int pageSize,
        RecruitmentStatus? status,
        CancellationToken ct
    )
    {
        var query = BaseQuery().Where(r => r.Recruiter.Id == recruiterId);
        query = status.HasValue
            ? query.Where(r => r.Status == status.Value)
            : query.Where(r => r.Status != RecruitmentStatus.Deleted);

        return await ToPagedResponseAsync(query.AsSplitQuery(), page, pageSize, ct);
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
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
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
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitment.Id);
        _embeddingSync.Schedule(EmbeddingTarget.User, recruiterId);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

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
        {
            throw new ForbiddenException("Only the recruitment recruiter can update it.");
        }

        if (request.Title is not null)
        {
            ValidateTitle(request.Title);
        }
        if (request.ExpiresAt.HasValue)
        {
            ValidateExpiry(request.ExpiresAt.Value);
        }
        if (request.Status.HasValue && !Enum.IsDefined(request.Status.Value))
        {
            throw new ValidationException("Recruitment status is invalid.");
        }
        var maxParticipants = request.MaxParticipants ?? recruitment.MaxParticipants;
        var expiresAt = request.ExpiresAt ?? recruitment.ExpiresAt;
        var status = request.Status ?? recruitment.Status;

        if (maxParticipants < recruitment.CurrParticipants)
        {
            throw new ValidationException(
                "Maximum participants cannot be less than current participants."
            );
        }

        if (status != RecruitmentStatus.Deleted && recruitment.CurrParticipants >= maxParticipants)
        {
            status = RecruitmentStatus.Closed;
        }

        if (status == RecruitmentStatus.Open && expiresAt <= DateTime.UtcNow)
        {
            throw new ConflictException("An expired recruitment cannot be opened.");
        }

        if (status == RecruitmentStatus.Open && recruitment.CurrParticipants >= maxParticipants)
        {
            throw new ConflictException("A full recruitment cannot be opened.");
        }

        var tagsChanged = request.RecruitmentTagIds is not null;
        if (tagsChanged)
        {
            recruitment.Tags = await GetTagsAsync(request.RecruitmentTagIds, ct);
        }

        recruitment.Update(
            request.Title?.Trim() ?? recruitment.Title,
            request.Description ?? recruitment.Description,
            maxParticipants,
            expiresAt,
            status
        );

        if (tagsChanged)
        {
            _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitment.Id);
            _embeddingSync.Schedule(
                EmbeddingTarget.User,
                await GetUsersAffectedByRecruitmentAsync(recruitment.Id, recruiterId, ct)
            );
        }
        else if (request.Status.HasValue)
        {
            _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitment.Id);
        }
        await _db.SaveChangesAsync(ct);

        return recruitment.ToResponse();
    }

    public async Task ForceCloseAsync(long recruitmentId, CancellationToken ct)
    {
        var recruitment =
            await _db.Recruitments.FirstOrDefaultAsync(
                item => item.Id == recruitmentId && item.Status != RecruitmentStatus.Deleted,
                ct
            ) ?? throw new NotFoundException("Recruitment not found.");

        recruitment.Delete();
        _embeddingSync.Schedule(EmbeddingTarget.Recruitment, recruitment.Id);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordViewAsync(long userId, long recruitmentId, CancellationToken ct)
    {
        const int maximumAttempts = 5;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct
            );
            try
            {
                var recruitment =
                    await _db
                        .Recruitments.Include(item => item.Recruiter)
                        .FirstOrDefaultAsync(
                            item =>
                                item.Id == recruitmentId
                                && item.Status != RecruitmentStatus.Deleted,
                            ct
                        )
                    ?? throw new NotFoundException("Recruitment not found.");

                if (recruitment.Recruiter.Id == userId)
                {
                    return;
                }

                var user =
                    await _db.Users.FindAsync([userId], ct)
                    ?? throw new NotFoundException("User not found.");
                var view = await _db.RecruitmentViews.FirstOrDefaultAsync(
                    item => item.UserId == userId && item.RecruitmentId == recruitmentId,
                    ct
                );
                if (view is null)
                {
                    view = new RecruitmentView(user, recruitment);
                    _db.RecruitmentViews.Add(view);
                }
                else
                {
                    view.RecordView();
                }

                if (view.ViewCount <= 3)
                {
                    _embeddingSync.Schedule(EmbeddingTarget.User, userId);
                }

                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            catch (Exception exception) when (IsViewConcurrencyFailure(exception))
            {
                await transaction.RollbackAsync(ct);
                _db.ChangeTracker.Clear();
                if (attempt == maximumAttempts)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(10 * attempt), ct);
            }
        }

        throw new ConflictException("The recruitment view could not be recorded concurrently.");
    }

    private IQueryable<Recruitment> BaseQuery(bool tracking = false)
    {
        var query = _db
            .Recruitments.Include(r => r.Game)
                .ThenInclude(g => g.Tags)
            .Include(r => r.Recruiter)
                .ThenInclude(u => u.Tags)
            .Include(r => r.Tags)
            .Include(r => r.Responses)
            .AsSplitQuery();

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
        {
            throw new NotFoundException("One or more recruitment tags do not exist.");
        }

        return tags;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("Recruitment title is required.");
        }
    }

    private static void ValidateExpiry(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ValidationException("Recruitment expiry must be in the future.");
        }
    }

    private static IQueryable<Recruitment> ApplySearchFilters(
        IQueryable<Recruitment> query,
        long userId,
        RecruitmentQueryRequest request,
        DateTime now
    )
    {
        query = query.Where(recruitment =>
            recruitment.Status == RecruitmentStatus.Open
            && recruitment.ExpiresAt > now
            && recruitment.CurrParticipants < recruitment.MaxParticipants
            && recruitment.Recruiter.Id != userId
            && !recruitment.Responses.Any(response => response.Responder.Id == userId)
        );
        if (request.GameId.HasValue)
        {
            query = query.Where(recruitment => recruitment.Game.Id == request.GameId.Value);
        }
        foreach (var tagId in request.GameTagIds?.Distinct() ?? [])
        {
            query = query.Where(recruitment => recruitment.Game.Tags.Any(tag => tag.Id == tagId));
        }
        foreach (var tagId in request.RecruitmentTagIds?.Distinct() ?? [])
        {
            query = query.Where(recruitment => recruitment.Tags.Any(tag => tag.Id == tagId));
        }
        return query;
    }

    private static bool IsViewConcurrencyFailure(Exception exception)
    {
        return exception
            is DbUpdateConcurrencyException
                or DbUpdateException
            {
                InnerException: MySqlException { Number: 1062 or 1205 or 1213 }
            }
                or MySqlException { Number: 1205 or 1213 };
    }

    private async Task<IReadOnlyCollection<long>> GetUsersAffectedByRecruitmentAsync(
        long recruitmentId,
        long recruiterId,
        CancellationToken ct
    )
    {
        var responders = _db
            .Responses.Where(response => response.Recruitment.Id == recruitmentId)
            .Select(response => response.Responder.Id);
        var viewers = _db
            .RecruitmentViews.Where(view => view.Recruitment.Id == recruitmentId)
            .Select(view => view.User.Id);
        var users = await responders.Concat(viewers).Distinct().ToListAsync(ct);
        users.Add(recruiterId);
        return users;
    }
}
