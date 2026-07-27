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
}