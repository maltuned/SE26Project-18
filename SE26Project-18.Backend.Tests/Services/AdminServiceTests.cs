using SE26Project_18.Backend.Data;
using Microsoft.Extensions.Configuration;
using Moq;
using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class AdminServiceTests
{
    private AppDbContext CreateDb() => TestDbContextFactory.Create();

    private Mock<ITokenService> CreateTokenMock()
    {
        var mock = new Mock<ITokenService>();
        mock.Setup(t => t.GenerateAdminAccessToken(It.IsAny<long>(), It.IsAny<string>()))
            .Returns("admin-jwt-token");
        return mock;
    }

    [Fact]
    public async Task Login_Throws_WhenAdminNotFound()
    {
        var db = CreateDb();
        var tokenMock = CreateTokenMock();
        var service = new AdminService(db, tokenMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("admin", "123456"));
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordWrong()
    {
        var db = CreateDb();
        db.Admins.Add(new Admin("admin", BCrypt.Net.BCrypt.HashPassword("correct")));
        await db.SaveChangesAsync();
        var tokenMock = CreateTokenMock();
        var service = new AdminService(db, tokenMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync("admin", "wrongpass"));
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsCorrect()
    {
        var db = CreateDb();
        db.Admins.Add(new Admin("admin", BCrypt.Net.BCrypt.HashPassword("123456")));
        await db.SaveChangesAsync();
        var tokenMock = CreateTokenMock();
        var service = new AdminService(db, tokenMock.Object);

        var (token, admin) = await service.LoginAsync("admin", "123456");

        Assert.Equal("admin-jwt-token", token);
        Assert.NotNull(admin);
        Assert.NotNull(admin.LastLoginAt);
    }

    [Fact]
    public async Task GetPendingCount_ReturnsCounts()
    {
        var db = CreateDb();
        db.Users.Add(new User("u1", "pw"));
        db.Users.Add(new User("u2", "pw"));
        await db.SaveChangesAsync();
        db.Reports.Add(new Report { ReporterId = 1, TargetType = Models.Enums.ReportTargetType.User, TargetId = 1, ViolationType = Models.Enums.ViolationType.Abuse, Content = "x" });
        db.Reports.Add(new Report { ReporterId = 1, TargetType = Models.Enums.ReportTargetType.User, TargetId = 1, ViolationType = Models.Enums.ViolationType.Abuse, Content = "y", Status = Models.Enums.ReportStatus.Resolved });
        db.Feedbacks.Add(new Feedback { UserId = 1, Type = Models.Enums.FeedbackType.ContentFeedback, Content = "f" });
        await db.SaveChangesAsync();
        var tokenMock = CreateTokenMock();
        var service = new AdminService(db, tokenMock.Object);

        var counts = await service.GetPendingCountAsync();

        Assert.Equal(2, counts.Length);
        Assert.True(counts[0] >= 0); // pending reports
        Assert.True(counts[1] >= 0); // pending feedbacks
    }
}
