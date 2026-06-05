using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Common;

public partial class CrudService
{
    public async Task<PagedResult<BookingDto>> GetBookingsCrudAsync(PaginationRequest request, CancellationToken ct)
    {
        var q = db.Bookings.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status }).ToListAsync(ct);
        return Page(i, request.PageNumber, request.PageSize, t);
    }

    public async Task<BookingDto> GetBookingAsync(long id, CancellationToken ct)
    {
        var x = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        return new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status };
    }

    public async Task DeleteBookingAsync(long id, CancellationToken ct)
    {
        var x = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        db.Bookings.Remove(x);
        await db.SaveChangesAsync(ct);
    }
}
