using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Entities;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

public sealed class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserResponse?> GetByIdAsync(long id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
            return null;

        return MapToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Tags)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (request.TagIds is { Count: > 0 })
        {
            user.Tags.Clear();

            var tags = await _db.UserTags
                .Where(t => request.TagIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var tag in tags)
                user.Tags.Add(tag);
        }

        user.UpdateProfile(request.Nickname, request.Signature, request.Gender);
        await _db.SaveChangesAsync(ct);

        return MapToResponse(user);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Signature = user.Signature,
            Gender = user.Gender,
            Status = user.Status,
            Tags = user.Tags.Select(t => new UserTagResponse
            {
                Id = t.Id,
                Name = t.Name,
            }).ToList(),
        };
    }
}