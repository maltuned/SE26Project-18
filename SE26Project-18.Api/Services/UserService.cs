using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Exceptions;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserResponse?> GetByIdAsync(long id, CancellationToken ct)
    {
        var user = await _db
            .Users.Include(u => u.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return null;

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

        await _db.SaveChangesAsync(ct);

        return user.ToResponse();
    }
}
