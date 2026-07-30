using Microsoft.EntityFrameworkCore;
using SE26Project_18.Api.Data;
using SE26Project_18.Api.Models.Enums;
using SE26Project_18.Api.Models.Mappings;
using SE26Project_18.Api.Models.Requests;
using SE26Project_18.Api.Models.Responses;

namespace SE26Project_18.Api.Services;

internal sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<UserResponse>> GetUsersAsync(
        AdminUserQueryRequest request,
        CancellationToken ct
    )
    {
        var query = _db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var search = request.Query.Trim();
            query = query.Where(user =>
                user.Username.Contains(search)
                || user.Nickname.Contains(search)
                || user.Signature.Contains(search)
            );
        }

        if (request.Status.HasValue)
        {
            query = query.Where(user => user.Status == request.Status.Value);
        }

        if (request.IsAdmin.HasValue)
        {
            var role = request.IsAdmin.Value ? UserRole.Admin : UserRole.User;
            query = query.Where(user => user.Role == role);
        }

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .OrderBy(user => user.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(user => user.Tags)
            .AsSplitQuery()
            .ToListAsync(ct);

        return CreatePage(
            users.Select(user => user.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            totalCount
        );
    }

    public async Task<PagedResponse<GameResponse>> GetGamesAsync(
        AdminGameQueryRequest request,
        CancellationToken ct
    )
    {
        var query = _db.Games.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var search = request.Query.Trim();
            query = query.Where(game =>
                game.Name.Contains(search) || game.Description.Contains(search)
            );
        }

        var totalCount = await query.CountAsync(ct);
        var games = await query
            .OrderBy(game => game.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(game => game.Tags)
            .AsSplitQuery()
            .ToListAsync(ct);

        return CreatePage(
            games.Select(game => game.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            totalCount
        );
    }

    public async Task<PagedResponse<RecruitmentResponse>> GetRecruitmentsAsync(
        AdminRecruitmentQueryRequest request,
        CancellationToken ct
    )
    {
        var query = _db.Recruitments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var search = request.Query.Trim();
            query = query.Where(recruitment =>
                recruitment.Title.Contains(search)
                || recruitment.Description.Contains(search)
                || recruitment.Recruiter.Username.Contains(search)
                || recruitment.Recruiter.Nickname.Contains(search)
                || recruitment.Game.Name.Contains(search)
            );
        }

        if (request.RecruiterId.HasValue)
        {
            query = query.Where(recruitment =>
                EF.Property<long>(recruitment, "RecruiterId") == request.RecruiterId.Value
            );
        }

        if (request.GameId.HasValue)
        {
            query = query.Where(recruitment =>
                EF.Property<long>(recruitment, "GameId") == request.GameId.Value
            );
        }

        if (request.Status.HasValue)
        {
            query = query.Where(recruitment => recruitment.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(ct);
        var ids = await query
            .OrderBy(recruitment => recruitment.Id)
            .Select(recruitment => recruitment.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        var recruitments = await _db
            .Recruitments.AsNoTracking()
            .Where(recruitment => ids.Contains(recruitment.Id))
            .OrderBy(recruitment => recruitment.Id)
            .Include(recruitment => recruitment.Game)
                .ThenInclude(game => game.Tags)
            .Include(recruitment => recruitment.Recruiter)
                .ThenInclude(user => user.Tags)
            .Include(recruitment => recruitment.Tags)
            .Include(recruitment => recruitment.Responses)
            .AsSplitQuery()
            .ToListAsync(ct);

        return CreatePage(
            recruitments.Select(recruitment => recruitment.ToResponse()).ToList(),
            request.Page,
            request.PageSize,
            totalCount
        );
    }

    private static PagedResponse<T> CreatePage<T>(
        IReadOnlyCollection<T> items,
        int page,
        int pageSize,
        int totalCount
    )
    {
        return new PagedResponse<T>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }
}
