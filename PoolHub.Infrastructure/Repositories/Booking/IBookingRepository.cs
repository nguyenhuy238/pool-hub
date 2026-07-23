namespace PoolHub.Infrastructure.Repositories;

public interface IBookingRepository
{
    Task<bool> CheckBookingConflictAsync(long? tableId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken ct);
}
