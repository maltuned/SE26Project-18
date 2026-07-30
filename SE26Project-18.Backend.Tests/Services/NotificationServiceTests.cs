using SE26Project_18.Backend.Models.Entities;
using SE26Project_18.Backend.Services;

namespace SE26Project_18.Backend.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task Create_CreatesNotification()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        var service = new NotificationService(db);

        var notification = await service.CreateAsync(user.Id, "Title", "Body text");

        Assert.Equal(user.Id, notification.UserId);
        Assert.Equal("Title", notification.Title);
        Assert.Equal("Body text", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task GetByUserId_ReturnsUserNotifications()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u1", "pw"));
        db.Users.Add(new User("u2", "pw"));
        await db.SaveChangesAsync();
        var u1 = db.Users.First();
        db.Notifications.Add(new Notification(u1.Id, "T1", "B1"));
        db.Notifications.Add(new Notification(u1.Id, "T2", "B2"));
        db.Notifications.Add(new Notification(2, "T3", "B3"));
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var result = await service.GetByUserIdAsync(u1.Id);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        db.Notifications.Add(new Notification(user.Id, "T1", "B1") { IsRead = false });
        db.Notifications.Add(new Notification(user.Id, "T2", "B2") { IsRead = false });
        db.Notifications.Add(new Notification(user.Id, "T3", "B3") { IsRead = true });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var count = await service.GetUnreadCountAsync(user.Id);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsFalse_WhenNotFound()
    {
        var db = TestDbContextFactory.Create();
        var service = new NotificationService(db);

        var result = await service.MarkAsReadAsync(999, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAsRead_SetsIsReadTrue()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        var n = new Notification(user.Id, "T", "B");
        db.Notifications.Add(n);
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var result = await service.MarkAsReadAsync(n.Id, user.Id);

        Assert.True(result);
        var updated = db.Notifications.First();
        Assert.True(updated.IsRead);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdateAsync")]
    public async Task MarkAllAsRead_MarksAllUnread()
    {
        var db = TestDbContextFactory.Create();
        db.Users.Add(new User("u", "pw"));
        await db.SaveChangesAsync();
        var user = db.Users.First();
        db.Notifications.Add(new Notification(user.Id, "T1", "B1") { IsRead = false });
        db.Notifications.Add(new Notification(user.Id, "T2", "B2") { IsRead = true });
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        await service.MarkAllAsReadAsync(user.Id);

        var unread = await service.GetUnreadCountAsync(user.Id);
        Assert.Equal(0, unread);
    }
}
