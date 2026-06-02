using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Booking;

public class BookingService(IBookingRepository repo) : IBookingService
{
    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };

    public async Task<PagedResult<BookingDto>> GetBookingsAsync(BookingFilterRequest request, CancellationToken ct)
    {
        var query = repo.GetBookings();
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.TableId.HasValue) query = query.Where(x => x.TableId == request.TableId.Value);
        if (request.Date.HasValue) query = query.Where(x => x.StartTimeUtc.Date == request.Date.Value.Date);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.BookingId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status }).ToListAsync(ct);
        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<BookingDto> GetBookingAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetBookingByIdAsync(id, ct) ?? throw new NotFoundException("Booking not found.");
        return new BookingDto { BookingId = x.BookingId, BookingCode = x.BookingCode, CustomerId = x.CustomerId, TableId = x.TableId, StartTimeUtc = x.StartTimeUtc, EndTimeUtc = x.EndTimeUtc, Status = x.Status };
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var conflict = await repo.HasOverlappingConfirmedBookingAsync(request.TableId, request.StartTimeUtc, request.EndTimeUtc, null, ct);
        if (conflict) throw new BusinessRuleException("Table is already booked and confirmed for this time slot.");

        int customerId = 0;
        if (request.CustomerId.HasValue && request.CustomerId.Value > 0)
        {
            customerId = request.CustomerId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var customer = await repo.GetCustomerByPhoneAsync(request.PhoneNumber, ct);
            if (customer == null)
            {
                customer = new PoolHub.Core.Entities.Customer { PhoneNumber = request.PhoneNumber, FullName = request.CustomerName ?? "Anonymous" };
                await repo.AddCustomerAsync(customer, ct);
                await repo.SaveChangesAsync(ct);
            }
            customerId = customer.CustomerId;
        }
        else
        {
            throw new BusinessRuleException("CustomerId or PhoneNumber is required.");
        }

        var entity = new PoolHub.Core.Entities.Booking
        {
            CustomerId = customerId,
            TableId = request.TableId,
            TableTypeId = request.TableTypeId,
            BookingCode = $"BK{DateTime.UtcNow:yyyyMMddHHmmss}",
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            NumberOfGuests = request.NumberOfGuests,
            Status = 1
        };
        await repo.AddBookingAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return new BookingDto { BookingId = entity.BookingId, BookingCode = entity.BookingCode, CustomerId = entity.CustomerId, TableId = entity.TableId, StartTimeUtc = entity.StartTimeUtc, EndTimeUtc = entity.EndTimeUtc, Status = entity.Status };
    }

    public async Task ConfirmAsync(int id, CancellationToken ct)
    {
        var booking = await repo.GetBookingByIdAsync(id, ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != 1) throw new BusinessRuleException("Only pending bookings can be confirmed.");
        
        var conflict = await repo.HasOverlappingConfirmedBookingAsync(booking.TableId, booking.StartTimeUtc, booking.EndTimeUtc, id, ct);
        if (conflict) throw new BusinessRuleException("Table is already booked and confirmed for this time slot.");
        
        booking.Status = 2;
        booking.ConfirmedAtUtc = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct)
    {
        var booking = await repo.GetBookingByIdAsync(id, ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status == 3 || booking.Status == 4) throw new BusinessRuleException("Booking is already cancelled or completed.");
        booking.Status = 3;
        booking.CancelledAtUtc = DateTime.UtcNow;
        await repo.SaveChangesAsync(ct);
    }

    public async Task DeleteBookingAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetBookingByIdAsync(id, ct) ?? throw new NotFoundException("Booking not found.");
        repo.RemoveBooking(x);
        await repo.SaveChangesAsync(ct);
    }
}
