using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetBookingsAsync(PaginationRequest request, CancellationToken ct);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct);
}
