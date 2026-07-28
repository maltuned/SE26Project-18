using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;

namespace SE26Project_18.Backend.Services;

public interface IReportService
{
    Task SubmitReportAsync(long reporterId, ReportTargetType targetType, long targetId, ViolationType violationType, string content);
    Task<List<Report>> GetAllAsync(ReportStatus? status = null);
    Task<Report?> GetByIdAsync(long id);
    Task<bool> UpdateStatusAsync(long id, ReportStatus status, long adminId);
}