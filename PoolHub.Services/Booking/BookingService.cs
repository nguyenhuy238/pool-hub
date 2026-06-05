using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using EntityBooking = PoolHub.Core.Entities.Booking;

namespace PoolHub.Services.Booking;

public class BookingService(PoolHubDbContext db) : IBookingService
{
    public async Task<PagedResult<BookingDto>> GetBookingsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.Bookings.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.BookingId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status }).ToListAsync(ct);
        return new PagedResult<BookingDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var entity = new EntityBooking
        {
            CustomerId = request.CustomerId,
            TableId = request.TableId,
            TableTypeId = request.TableTypeId,
            BookingCode = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}",
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            NumberOfGuests = request.NumberOfGuests,
            Status = 1
        };
        db.Bookings.Add(entity);
        await db.SaveChangesAsync(ct);
        return new BookingDto { BookingId = entity.BookingId, BookingCode = entity.BookingCode, CustomerId = entity.CustomerId, TableId = entity.TableId, StartTimeUtc = entity.StartTimeUtc, EndTimeUtc = entity.EndTimeUtc, Status = entity.Status };
    }
}
