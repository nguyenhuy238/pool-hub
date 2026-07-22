using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class NotificationRepository(PoolHubDbContext db) : INotificationRepository
{
    public Task<List<Notification>> GetUnreadByUserAsync(long userId, CancellationToken ct) =>
        db.Notifications.Where(x => x.UserId == userId && !x.IsRead).ToListAsync(ct);
}
