using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AdminService(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<(string Token, Admin Admin)> LoginAsync(string username, string password)
    {
        var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Username == username);
        if (admin == null)
            throw new InvalidOperationException("管理员不存在");

        if (!BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
            throw new InvalidOperationException("密码错误");

        admin.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateAdminAccessToken(admin.Id, admin.Username);
        return (token, admin);
    }

    public async Task<int[]> GetPendingCountAsync()
    {
        var pendingReports = await _db.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
        var pendingFeedbacks = await _db.Feedbacks.CountAsync(f => f.Status == FeedbackStatus.Pending);
        return [pendingReports, pendingFeedbacks];
    }
}
