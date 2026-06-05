using Microsoft.EntityFrameworkCore;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public interface IBookingRepository
{
    Task<bool> CheckBookingConflictAsync(long? tableId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken ct);
}

public class BookingRepository(PoolHubDbContext db) : IBookingRepository
{
    public Task<bool> CheckBookingConflictAsync(long? tableId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken ct) =>
        db.Bookings.AnyAsync(x => tableId.HasValue && x.TableId == tableId && x.StartTimeUtc < endTimeUtc && startTimeUtc < x.EndTimeUtc, ct);
}
