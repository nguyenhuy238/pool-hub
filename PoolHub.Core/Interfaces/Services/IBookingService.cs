using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetBookingsAsync(BookingFilterRequest request, CancellationToken ct);
    Task<BookingDto> GetBookingAsync(int id, CancellationToken ct);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct);
    Task ConfirmAsync(int id, CancellationToken ct);
    Task CancelAsync(int id, CancellationToken ct);
    Task DeleteBookingAsync(int id, CancellationToken ct);
}
