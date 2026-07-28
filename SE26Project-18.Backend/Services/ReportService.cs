using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SubmitReportAsync(long reporterId, ReportTargetType targetType, long targetId, ViolationType violationType, string content)
    {
        var report = new Report
        {
            ReporterId = reporterId,
            TargetType = targetType,
            TargetId = targetId,
            ViolationType = violationType,
            Content = content,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();
    }

    public async Task<List<Report>> GetAllAsync(ReportStatus? status = null)
    {
        var query = _db.Reports.Include(r => r.Reporter).AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<Report?> GetByIdAsync(long id)
    {
        return await _db.Reports.Include(r => r.Reporter).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> UpdateStatusAsync(long id, ReportStatus status, long adminId)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null) return false;

        report.Status = status;
        report.HandledAt = DateTime.UtcNow;
        report.HandledByAdminId = adminId;
        await _db.SaveChangesAsync();
        return true;
    }
}