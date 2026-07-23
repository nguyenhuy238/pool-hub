using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetUnreadByUserAsync(long userId, CancellationToken ct);
}
