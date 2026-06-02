using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Notification;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services;

public class NotificationService(PoolHubDbContext db) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(int userId, PaginationRequest request, CancellationToken ct)
    {
        var query = db.Notifications.Where(x => x.UserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.NotificationId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new NotificationDto
            {
                NotificationId = x.NotificationId,
                Title = x.Title,
                Content = x.Content,
                IsRead = x.IsRead,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
        return new PagedResult<NotificationDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task MarkAsReadAsync(int notificationId, int userId, CancellationToken ct)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == userId, ct)
            ?? throw new NotFoundException("Notification not found.");
        notification.IsRead = true;
        notification.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken ct)
    {
        await db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), ct);
    }

    public async Task CreateNotificationAsync(int userId, string title, string content, CancellationToken ct)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            IsRead = false
        });
        await db.SaveChangesAsync(ct);
    }
}
