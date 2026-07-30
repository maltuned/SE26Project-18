using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Enums;

namespace SE26Project_18.Api.Infrastructure.Authentication;

internal sealed class AccessTokenUserValidator
{
    private readonly AppDbContext _db;

    public AccessTokenUserValidator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsAllowedAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return false;
        }

        return await _db.Users.AnyAsync(
            user => user.Id == userId && user.Status != UserStatus.Suspended,
            ct
        );
    }
}
