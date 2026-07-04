using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Notification;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Notification;

public class NotificationService(PoolHubDbContext db, IClock? clock = null) : INotificationService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;
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

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(long userId, NotificationQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Notifications.AsNoTracking()
            .Where(x => x.UserId == userId || x.UserId == null);
        if (!string.IsNullOrWhiteSpace(request.Type)) query = query.Where(x => x.NotificationType == request.Type);
        if (request.IsRead.HasValue) query = query.Where(x => x.IsRead == request.IsRead);
        var total = await query.CountAsync(ct);
        var items = await Project(query).OrderByDescending(x => x.NotificationId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        return new PagedResult<NotificationDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalItems = total };
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(NotificationQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Notifications.AsNoTracking().AsQueryable();
        if (request.UserId.HasValue) query = query.Where(x => x.UserId == request.UserId);
        if (request.CustomerId.HasValue) query = query.Where(x => x.CustomerId == request.CustomerId);
        if (!string.IsNullOrWhiteSpace(request.Type)) query = query.Where(x => x.NotificationType == request.Type);
        if (request.IsRead.HasValue) query = query.Where(x => x.IsRead == request.IsRead);
        var total = await query.CountAsync(ct);
        var items = await Project(query).OrderByDescending(x => x.NotificationId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return new PagedResult<NotificationDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalItems = total };
    }

    public async Task<NotificationDto> GetNotificationAsync(long id, long userId, bool isAdmin, CancellationToken ct)
    {
        var query = db.Notifications.AsNoTracking().Where(x => x.NotificationId == id);
        if (!isAdmin) query = query.Where(x => x.UserId == null || x.UserId == userId);
        return await Project(query).FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Notification not found.");
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            throw new ValidationException("Title and message are required.");
        var entity = new PoolHub.Core.Entities.Notification {
            UserId = request.UserId, CustomerId = request.CustomerId, Title = request.Title.Trim(),
            Message = request.Message.Trim(), NotificationType = request.NotificationType.Trim().ToUpperInvariant()
        };
        db.Notifications.Add(entity);
        await db.SaveChangesAsync(ct);
        return await Project(db.Notifications.AsNoTracking().Where(x => x.NotificationId == entity.NotificationId)).SingleAsync(ct);
    }

    public async Task MarkReadAsync(long id, long userId, bool isAdmin, CancellationToken ct)
    {
        var entity = await db.Notifications.FindAsync([id], ct) ?? throw new NotFoundException("Notification not found.");
        if (!isAdmin && entity.UserId.HasValue && entity.UserId != userId) throw new ForbiddenException("Cannot update another user's notification.");
        entity.IsRead = true;
        entity.ReadAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(long userId, bool isAdmin, CancellationToken ct)
    {
        var query = db.Notifications.Where(x => !x.IsRead);
        if (!isAdmin) query = query.Where(x => x.UserId == null || x.UserId == userId);
        var items = await query.ToListAsync(ct);
        var now = _clock.UtcNow;
        foreach (var item in items) { item.IsRead = true; item.ReadAtUtc = now; }
        await db.SaveChangesAsync(ct);
    }

    private static IQueryable<NotificationDto> Project(IQueryable<PoolHub.Core.Entities.Notification> query) =>
        query.Select(x => new NotificationDto {
            NotificationId = x.NotificationId, UserId = x.UserId, CustomerId = x.CustomerId, Title = x.Title,
            Message = x.Message, NotificationType = x.NotificationType, IsRead = x.IsRead,
            ReadAtUtc = x.ReadAtUtc, CreatedAtUtc = x.CreatedAtUtc
        });

    public async Task DeleteAsync(long id, long userId, bool isAdmin, CancellationToken ct)
    {
        var entity = await db.Notifications.FindAsync([id], ct) ?? throw new NotFoundException("Notification not found.");
        if (!isAdmin && entity.UserId.HasValue && entity.UserId != userId) throw new ForbiddenException("Cannot delete another user's notification.");
        db.Notifications.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct)
    {
        return db.Notifications.CountAsync(x => !x.IsRead && (x.UserId == userId || x.UserId == null), ct);
    }
}
