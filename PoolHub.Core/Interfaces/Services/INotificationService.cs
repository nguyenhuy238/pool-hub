using PoolHub.Core.DTOs.Notification;

namespace PoolHub.Core.Interfaces.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken ct);
}
