using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Models.Enums;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class ReportServiceTests
{
    [Fact]
    public async Task SubmitReport_CreatesAndSaves()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("reporter", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        var service = new ReportService(db);

        await service.SubmitReportAsync(user.Id, ReportTargetType.Recruitment, 10, ViolationType.Abuse, "Bad behavior");

        var report = db.Reports.First();
        Assert.Equal(user.Id, report.ReporterId);
        Assert.Equal(ReportTargetType.Recruitment, report.TargetType);
        Assert.Equal(10L, report.TargetId);
        Assert.Equal(ViolationType.Abuse, report.ViolationType);
        Assert.Equal("Bad behavior", report.Content);
        Assert.Equal(ReportStatus.Pending, report.Status);
    }

    [Fact]
    public async Task GetAll_ReturnsAll()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        db.Reports.Add(new Report { ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 1, ViolationType = ViolationType.Abuse, Content = "x" });
        db.Reports.Add(new Report { ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 2, ViolationType = ViolationType.Fraud, Content = "y" });
        await db.SaveChangesAsync();
        var service = new ReportService(db);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAll_FiltersByStatus()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        db.Reports.Add(new Report { ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 1, ViolationType = ViolationType.Abuse, Content = "x", Status = ReportStatus.Pending });
        db.Reports.Add(new Report { ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 2, ViolationType = ViolationType.Abuse, Content = "y", Status = ReportStatus.Resolved });
        await db.SaveChangesAsync();
        var service = new ReportService(db);

        var pending = await service.GetAllAsync(ReportStatus.Pending);
        var resolved = await service.GetAllAsync(ReportStatus.Resolved);

        Assert.Single(pending);
        Assert.Single(resolved);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenNotFound()
    {
        var db = TestDbContextFactory.Create();
        var service = new ReportService(db);

        var result = await service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsFalse_WhenNotFound()
    {
        var db = TestDbContextFactory.Create();
        var service = new ReportService(db);

        var result = await service.UpdateStatusAsync(999, ReportStatus.Resolved, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateStatus_SetsStatusAndAdmin()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        db.Reports.Add(new Report { ReporterId = 1, TargetType = ReportTargetType.User, TargetId = 1, ViolationType = ViolationType.Abuse, Content = "test" });
        await db.SaveChangesAsync();
        var service = new ReportService(db);
        var report = db.Reports.First();

        var result = await service.UpdateStatusAsync(report.Id, ReportStatus.Resolved, 3);

        Assert.True(result);
        var updated = db.Reports.First();
        Assert.Equal(ReportStatus.Resolved, updated.Status);
        Assert.Equal(3L, updated.HandledByAdminId);
    }
}
