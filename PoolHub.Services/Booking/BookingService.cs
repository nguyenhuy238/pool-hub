using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;
using EntityBooking = PoolHub.Core.Entities.Booking;
using EntityCustomer = PoolHub.Core.Entities.Customer;

namespace PoolHub.Services.Booking;

public class BookingService(PoolHubDbContext db) : IBookingService
{
    public async Task<PagedResult<BookingDto>> GetBookingsAsync(BookingQueryRequest request, CancellationToken ct)
    {
        var query = db.Bookings.AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        
        if (request.Date.HasValue)
            query = query.Where(x => x.StartTimeUtc.Date == request.Date.Value.Date);

        if (request.TableId.HasValue)
            query = query.Where(x => x.TableId == request.TableId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.BookingId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status }).ToListAsync(ct);
        return new PagedResult<BookingDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        long customerId = 0;

        if (request.CustomerId.HasValue && request.CustomerId > 0)
        {
            customerId = request.CustomerId.Value;
        }
        else if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber, ct);
            if (customer != null)
            {
                customerId = customer.CustomerId;
            }
            else
            {
                var newCustomer = new EntityCustomer { PhoneNumber = request.PhoneNumber, FullName = request.CustomerName ?? "Anonymous" };
                db.Customers.Add(newCustomer);
                await db.SaveChangesAsync(ct);
                customerId = newCustomer.CustomerId;
            }
        }
        else
        {
            throw new Exception("Either CustomerId or PhoneNumber must be provided.");
        }

        if (request.StartTimeUtc.Minute % 30 != 0 || request.StartTimeUtc.Second != 0 || request.StartTimeUtc.Millisecond != 0 ||
            request.EndTimeUtc.Minute % 30 != 0 || request.EndTimeUtc.Second != 0 || request.EndTimeUtc.Millisecond != 0)
        {
            throw new BusinessRuleException("Thời gian đặt bàn phải là các mốc chẵn 30 phút (VD: 10:00, 10:30).");
        }

        if (request.TableId.HasValue)
        {
            var isConflict = await db.Bookings.AnyAsync(b => 
                b.TableId == request.TableId.Value && 
                b.Status == 2 && // Confirmed
                b.StartTimeUtc < request.EndTimeUtc && 
                b.EndTimeUtc > request.StartTimeUtc, ct);

            if (isConflict)
            {
                throw new Exception("Table is already booked and confirmed for the selected time.");
            }
        }

        var entity = new EntityBooking
        {
            CustomerId = customerId,
            TableId = request.TableId,
            TableTypeId = request.TableTypeId,
            BookingCode = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}",
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            NumberOfGuests = request.NumberOfGuests,
            Status = 2, // Confirmed (Auto-confirm)
            ConfirmedAtUtc = DateTime.UtcNow
        };
        db.Bookings.Add(entity);
        await db.SaveChangesAsync(ct);
        return new BookingDto { BookingId = entity.BookingId, BookingCode = entity.BookingCode, CustomerId = entity.CustomerId, TableId = entity.TableId, StartTimeUtc = entity.StartTimeUtc, EndTimeUtc = entity.EndTimeUtc, Status = entity.Status };
    }

    public async Task<BookingDto> ConfirmAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != 1) throw new BusinessRuleException("Only Pending bookings can be confirmed.");
        if (booking.TableId.HasValue && await HasConflictAsync(booking, ct))
            throw new ConflictException("Table is already booked for the selected time.");

        booking.Status = 2; // Confirmed
        booking.ConfirmedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new BookingDto { BookingId = booking.BookingId, BookingCode = booking.BookingCode, CustomerId = booking.CustomerId, TableId = booking.TableId, StartTimeUtc = booking.StartTimeUtc, EndTimeUtc = booking.EndTimeUtc, Status = booking.Status };
    }

    public async Task<BookingDto> CancelAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status == 3 || booking.Status == 4 || booking.Status == 5) throw new BusinessRuleException("Booking cannot be cancelled.");

        booking.Status = 3; // Cancelled
        booking.CancelledAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new BookingDto { BookingId = booking.BookingId, BookingCode = booking.BookingCode, CustomerId = booking.CustomerId, TableId = booking.TableId, StartTimeUtc = booking.StartTimeUtc, EndTimeUtc = booking.EndTimeUtc, Status = booking.Status };
    }

    public async Task<BookingDto> MarkNoShowAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != 2) throw new BusinessRuleException("Only Confirmed bookings can be marked NoShow.");
        booking.Status = 5;
        await db.SaveChangesAsync(ct);
        return Map(booking);
    }

    public async Task<BookingDto> MarkCompletedAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != 2) throw new BusinessRuleException("Only Confirmed bookings can be completed.");
        booking.Status = 4;
        await db.SaveChangesAsync(ct);
        return Map(booking);
    }

    public async Task<List<AvailableTableDto>> GetAvailabilityAsync(BookingAvailabilityRequest request, CancellationToken ct)
    {
        if (request.EndTimeUtc <= request.StartTimeUtc) throw new ValidationException("End time must be after start time.");
        var query = from table in db.VenueTables.AsNoTracking()
                    join type in db.TableTypes.AsNoTracking() on table.TableTypeId equals type.TableTypeId
                    where table.OperationalStatus == 1
                       && !db.SessionTableAssignments.Any(a => a.TableId == table.TableId && a.EndedAtUtc == null)
                       && !db.Bookings.Any(b => b.TableId == table.TableId && b.Status == 2
                            && b.StartTimeUtc < request.EndTimeUtc && b.EndTimeUtc > request.StartTimeUtc)
                    select new AvailableTableDto {
                        TableId = table.TableId, TableCode = table.TableCode, TableName = table.TableName,
                        TableTypeId = table.TableTypeId, TableTypeName = type.Name, Capacity = table.Capacity
                    };
        if (request.TableTypeId.HasValue) query = query.Where(x => x.TableTypeId == request.TableTypeId);
        return await query.OrderBy(x => x.TableCode).ToListAsync(ct);
    }

    private Task<bool> HasConflictAsync(EntityBooking booking, CancellationToken ct) =>
        db.Bookings.AnyAsync(x => x.BookingId != booking.BookingId && x.TableId == booking.TableId && x.Status == 2
            && x.StartTimeUtc < booking.EndTimeUtc && x.EndTimeUtc > booking.StartTimeUtc, ct);

    private static BookingDto Map(EntityBooking booking) => new() {
        BookingId = booking.BookingId, BookingCode = booking.BookingCode, CustomerId = booking.CustomerId,
        TableId = booking.TableId, StartTimeUtc = booking.StartTimeUtc, EndTimeUtc = booking.EndTimeUtc, Status = booking.Status
    };

    /// <inheritdoc/>
    public async Task<PagedResult<BookingCalendarItem>> GetCalendarAsync(BookingCalendarRequest request, CancellationToken ct)
    {
        // Validate khoảng thời gian
        if (request.From > request.To)
            throw new ValidationException("'from' must be earlier than 'to'.");

        // Giới hạn pageSize tối đa 200
        var pageSize = Math.Min(request.PageSize, 200);

        var query = db.Bookings
            .Where(b => b.StartTimeUtc < request.To && b.EndTimeUtc > request.From);

        if (request.TableId.HasValue)
            query = query.Where(b => b.TableId == request.TableId.Value);

        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        // Join với Customer và VenueTable để lấy tên đầy đủ
        var items = await query
            .OrderBy(b => b.StartTimeUtc)
            .Skip((request.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                Booking = b,
                CustomerName = db.Customers
                    .Where(c => c.CustomerId == b.CustomerId)
                    .Select(c => c.FullName)
                    .FirstOrDefault() ?? string.Empty,
                CustomerPhone = db.Customers
                    .Where(c => c.CustomerId == b.CustomerId)
                    .Select(c => c.PhoneNumber)
                    .FirstOrDefault() ?? string.Empty,
                TableCode = b.TableId != null
                    ? db.VenueTables.Where(t => t.TableId == b.TableId).Select(t => t.TableCode).FirstOrDefault()
                    : null,
                TableName = b.TableId != null
                    ? db.VenueTables.Where(t => t.TableId == b.TableId).Select(t => t.TableName).FirstOrDefault()
                    : null,
                TableTypeName = b.TableTypeId != null
                    ? db.TableTypes.Where(tt => tt.TableTypeId == b.TableTypeId).Select(tt => tt.Name).FirstOrDefault()
                    : null
            })
            .Select(x => new BookingCalendarItem
            {
                BookingId = x.Booking.BookingId,
                BookingCode = x.Booking.BookingCode,
                CustomerId = x.Booking.CustomerId,
                CustomerName = x.CustomerName,
                CustomerPhone = x.CustomerPhone,
                TableId = x.Booking.TableId,
                TableCode = x.TableCode,
                TableName = x.TableName,
                TableTypeId = x.Booking.TableTypeId,
                TableTypeName = x.TableTypeName,
                StartTimeUtc = x.Booking.StartTimeUtc,
                EndTimeUtc = x.Booking.EndTimeUtc,
                NumberOfGuests = x.Booking.NumberOfGuests,
                Status = x.Booking.Status,
                Note = x.Booking.Note,
                ConfirmedAtUtc = x.Booking.ConfirmedAtUtc,
                CancelledAtUtc = x.Booking.CancelledAtUtc
            })
            .ToListAsync(ct);

        return new PagedResult<BookingCalendarItem>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PublicBookingSlotDto>> GetPublicCalendarAsync(long tableId, DateTime date, CancellationToken ct)
    {
        // Khoảng thời gian trong ngày (từ 00:00 đến 23:59 của ngày đó - theo UTC hoặc local tùy thuộc logic lưu trữ của DB, 
        // ở đây ta so sánh StartTimeUtc/EndTimeUtc có giao với ngày được chỉ định).
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        // Lấy các Booking của TableId này trong ngày, với Status = 1 (Pending) hoặc 2 (Confirmed)
        var slots = await db.Bookings
            .Where(b => b.TableId == tableId && 
                        (b.Status == 1 || b.Status == 2) && 
                        b.StartTimeUtc < endOfDay && 
                        b.EndTimeUtc > startOfDay)
            .OrderBy(b => b.StartTimeUtc)
            .Select(b => new PublicBookingSlotDto
            {
                StartTimeUtc = b.StartTimeUtc,
                EndTimeUtc = b.EndTimeUtc
            })
            .ToListAsync(ct);

        return slots;
    }
}
