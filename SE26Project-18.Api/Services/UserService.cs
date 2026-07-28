using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Infrastructure.Embedding;
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

    public UserService(AppDbContext db, IEmbeddingSyncScheduler embeddingSync)
    {
        _db = db;
        _embeddingSync = embeddingSync;
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
