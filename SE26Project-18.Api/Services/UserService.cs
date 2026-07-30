using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
using SE26Project_18.Api.Infrastructure.Realtime;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;
using SE26Project_18.Api.Services.Recommendations;

namespace SE26Project_18.Api.Services;

internal sealed class UserService : IUserService
{
    private readonly AppDbContext _db;

    private readonly IEmbeddingSyncScheduler _embeddingSync;

    private readonly IMessageConnectionManager _connectionManager;

    public UserService(
        AppDbContext db,
        IEmbeddingSyncScheduler embeddingSync,
        IMessageConnectionManager connectionManager
    )
    {
        _db = db;
        _embeddingSync = embeddingSync;
        _connectionManager = connectionManager;
    }

    public async Task EnsureActiveAsync(long id, CancellationToken ct)
    {
        if (
            !await _db.Users.AsNoTracking()
                .AnyAsync(user => user.Id == id && user.Status != UserStatus.Suspended, ct)
        )
        {
            throw new AuthenticationException("User is unavailable or suspended.");
        }
    }

    public async Task<UserResponse?> GetByIdAsync(long id, CancellationToken ct)
    {
        var user = await _db
            .Users.Include(u => u.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return null;
        }

        return user.ToResponse();
    }

    public async Task<UserResponse> UpdateAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken ct
    )
    {
        var user =
            await _db.Users.Include(u => u.Tags).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User not found.");

        var tagIds = request.TagIds?.Distinct().ToArray();

        List<UserTag>? tags = null;

        if (tagIds is not null)
        {
            tags = await _db.UserTags.Where(t => tagIds.Contains(t.Id)).ToListAsync(ct);

            if (tags.Count != tagIds.Length)
            {
                throw new NotFoundException("One or more tags do not exist.");
            }
        }

        request.ApplyTo(user, tags);

        if (request.TagIds is not null)
        {
            var affectedUserIds = await GetUsersInterestedInRecruiterAsync(id, ct);
            _embeddingSync.Schedule(EmbeddingTarget.User, affectedUserIds.Append(id));
        }

        await _db.SaveChangesAsync(ct);

        return user.ToResponse();
    }

    public async Task<UserResponse> SetSuspensionAsync(
        long actorId,
        long id,
        SetUserSuspensionRequest request,
        CancellationToken ct
    )
    {
        var user =
            await _db.Users.Include(u => u.Tags).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User not found.");

        if (actorId == id)
        {
            throw new ForbiddenException("Administrators cannot suspend themselves.");
        }

        if (user.Role == UserRole.Admin)
        {
            throw new ForbiddenException("Administrators cannot be suspended.");
        }

        user.Status = request.Suspended ? UserStatus.Suspended : UserStatus.Offline;

        if (request.Suspended)
        {
            var refreshTokens = await _db
                .RefreshTokens.Where(token => token.UserId == id && !token.IsRevoked)
                .ToListAsync(ct);
            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.IsRevoked = true;
            }
        }

        await _db.SaveChangesAsync(ct);
        if (request.Suspended)
        {
            await _connectionManager.CloseUserAsync(id);
        }
        else
        {
            _connectionManager.AllowUser(id);
        }

        return user.ToResponse();
    }

    private async Task<IReadOnlyCollection<long>> GetUsersInterestedInRecruiterAsync(
        long recruiterId,
        CancellationToken ct
    )
    {
        var responders = _db
            .Responses.Where(response => response.Recruitment.Recruiter.Id == recruiterId)
            .Select(response => response.Responder.Id);
        var viewers = _db
            .RecruitmentViews.Where(view => view.Recruitment.Recruiter.Id == recruiterId)
            .Select(view => view.User.Id);
        var acceptedByRecruiters = _db
            .Responses.Where(response =>
                response.Responder.Id == recruiterId && response.Type == ResponseType.Accepted
            )
            .Select(response => response.Recruitment.Recruiter.Id);

        return await responders
            .Concat(viewers)
            .Concat(acceptedByRecruiters)
            .Distinct()
            .ToListAsync(ct);
    }
}
