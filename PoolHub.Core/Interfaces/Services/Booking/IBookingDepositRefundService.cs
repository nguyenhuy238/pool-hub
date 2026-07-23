using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Services;

public interface IBookingDepositRefundService
{
    Task<decimal> CalculateRefundableBalanceAsync(long bookingDepositId, CancellationToken ct);
    Task<BookingDepositRefundDto> CreateRefundRequestAsync(CreateBookingDepositRefundRequest request, CancellationToken ct);
    Task<BookingDepositRefundDto?> CreateEligibleCancellationRefundAsync(long bookingDepositId, long? requestedByUserId, CancellationToken ct);
    Task<BookingDepositRefundDto?> CreateVenueFaultRefundAsync(long bookingDepositId, long? requestedByUserId, CancellationToken ct);
    Task<BookingDepositRefundDto?> CreateDepositExcessRefundAsync(long bookingDepositId, long invoiceId, decimal amount, CancellationToken ct);
    Task<BookingDepositRefundDto> ApproveAsync(long refundId, long approvedByUserId, CancellationToken ct);
    Task<BookingDepositRefundDto> RejectAsync(long refundId, long rejectedByUserId, string reason, CancellationToken ct);
    Task<BookingDepositRefundDto> MarkProcessingAsync(long refundId, long processedByUserId, CancellationToken ct);
    Task<BookingDepositRefundDto> MarkFailedAsync(long refundId, long processedByUserId, string reason, CancellationToken ct);
    Task<BookingDepositRefundDto> CompleteAsync(long refundId, long processedByUserId, string? transferCode, CancellationToken ct);
    Task<DepositApplicationResult> ApplyDepositToInvoiceAsync(Session session, Invoice invoice, CancellationToken ct);
}
