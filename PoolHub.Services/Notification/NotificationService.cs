using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Notification;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Services.Notification;

public class NotificationService(PoolHubDbContext db) : INotificationService
{
    public Task<List<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken ct)
        => db.Notifications
            .Where(x => x.UserId == userId || x.UserId == null)
            .OrderByDescending(x => x.NotificationId)
            .Select(x => new NotificationDto
            {
                NotificationId = x.NotificationId,
                UserId = x.UserId,
                CustomerId = x.CustomerId,
                Title = x.Title,
                Message = x.Message,
                NotificationType = x.NotificationType,
                IsRead = x.IsRead,
                ReadAtUtc = x.ReadAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
}
