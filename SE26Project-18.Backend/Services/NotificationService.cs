using Microsoft.EntityFrameworkCore;
using SE26Project_18.Backend.Data;
using SE26Project_18.Backend.Models.Dtos;
using SE26Project_18.Backend.Models.Entities;

namespace SE26Project_18.Backend.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetByUserIdAsync(long userId);
    Task<int> GetUnreadCountAsync(long userId);
    Task<bool> MarkAsReadAsync(long notificationId, long userId);
    Task MarkAllAsReadAsync(long userId);
    Task<Notification> CreateAsync(long userId, string title, string body);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<NotificationDto>> GetByUserIdAsync(long userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(long notificationId, long userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null)
            return false;
        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task<Notification> CreateAsync(long userId, string title, string body)
    {
        var notification = new Notification(userId, title, body);
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }
}