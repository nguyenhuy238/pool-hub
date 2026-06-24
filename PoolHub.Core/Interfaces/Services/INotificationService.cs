using PoolHub.Core.DTOs.Notification;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface INotificationService
{
    Task<List<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken ct);
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(long userId, NotificationQueryRequest request, CancellationToken ct);
    Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationQueryRequest request, CancellationToken ct);
    Task<NotificationDto> GetNotificationAsync(long id, long userId, bool isAdmin, CancellationToken ct);
    Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken ct);
    Task MarkReadAsync(long id, long userId, bool isAdmin, CancellationToken ct);
    Task MarkAllReadAsync(long userId, bool isAdmin, CancellationToken ct);
    Task DeleteAsync(long id, long userId, bool isAdmin, CancellationToken ct);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct);
}
