using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IBookingService
{
    Task<PagedResult<BookingDto>> GetBookingsAsync(BookingQueryRequest request, CancellationToken ct);
    Task<BookingDto> GetByIdAsync(long id, CancellationToken ct);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct);
    Task<BookingDto> CreatePublicAsync(CreateBookingRequest request, CancellationToken ct);
    Task<BookingDto> UpdateAsync(long id, UpdateBookingRequest request, CancellationToken ct);
    Task<BookingDto> ConfirmAsync(long id, long? confirmedByUserId, CancellationToken ct);
    Task<BookingDto> ApproveAsync(long id, long approvedByUserId, CancellationToken ct);
    Task<BookingDto> SubmitDepositTransferAsync(long id, CancellationToken ct);
    Task<BookingDto> ConfirmDepositAsync(long id, ConfirmDepositRequest request, long confirmedByUserId, CancellationToken ct);
    Task<BookingDto> RejectDepositTransferAsync(long id, RejectDepositTransferRequest request, long rejectedByUserId, CancellationToken ct);
    Task<BookingDto> MockPayDepositAsync(long id, CancellationToken ct);
    Task<BookingDto> CancelAsync(long id, CancelBookingRequest request, CancellationToken ct);
    Task<BookingDto> MarkNoShowAsync(long id, NoShowBookingRequest request, CancellationToken ct);
    Task<BookingDto> MarkCompletedAsync(long id, CancellationToken ct);
    Task<int> ExpirePendingDepositsAsync(DateTime nowUtc, CancellationToken ct);
    Task<List<AvailableTableDto>> GetAvailabilityAsync(BookingAvailabilityRequest request, CancellationToken ct);

    /// <summary>
    /// Lấy danh sách booking theo khoảng thời gian, dùng cho Calendar View.
    /// Kết quả bao gồm thông tin khách hàng và bàn đầy đủ.
    /// </summary>
    Task<PagedResult<BookingCalendarItem>> GetCalendarAsync(BookingCalendarRequest request, CancellationToken ct);

    /// <summary>
    /// Lấy danh sách các khung giờ đã bị đặt của một bàn trong một ngày (Public API).
    /// </summary>
    Task<IEnumerable<PublicBookingSlotDto>> GetPublicCalendarAsync(long tableId, DateTime date, CancellationToken ct);
}
