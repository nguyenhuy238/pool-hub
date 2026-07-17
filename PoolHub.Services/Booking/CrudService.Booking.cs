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
        var i = await q
            .OrderByDescending(x => x.BookingId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new BookingDto
            {
                BookingId = x.BookingId,
                BookingCode = x.BookingCode,
                CustomerId = x.CustomerId,
                CustomerName = db.Customers.Where(c => c.CustomerId == x.CustomerId).Select(c => c.FullName).FirstOrDefault() ?? string.Empty,
                PhoneNumber = db.Customers.Where(c => c.CustomerId == x.CustomerId).Select(c => c.PhoneNumber).FirstOrDefault() ?? string.Empty,
                TableId = x.TableId,
                TableTypeId = x.TableTypeId,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                EstimatedAmount = x.EstimatedAmount,
                Status = x.Status
            })
            .ToListAsync(ct);
        return Page(i, request.PageNumber, request.PageSize, t);
    }

    public async Task<BookingDto> GetBookingAsync(long id, CancellationToken ct)
    {
        var x = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == x.CustomerId, ct);
        return new BookingDto
        {
            BookingId = x.BookingId,
            BookingCode = x.BookingCode,
            CustomerId = x.CustomerId,
            CustomerName = customer?.FullName ?? string.Empty,
            PhoneNumber = customer?.PhoneNumber ?? string.Empty,
            TableId = x.TableId,
            TableTypeId = x.TableTypeId,
            StartTimeUtc = x.StartTimeUtc,
            EndTimeUtc = x.EndTimeUtc,
            EstimatedAmount = x.EstimatedAmount,
            Status = x.Status
        };
    }

    public async Task DeleteBookingAsync(long id, CancellationToken ct)
    {
        var x = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        db.Bookings.Remove(x);
        await db.SaveChangesAsync(ct);
    }
}
