using PoolHub.Core.DTOs.BookingDepositRefund;
using PoolHub.Core.Entities;
using PoolHub.Shared;

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
    Task<DepositRefundSummaryDto?> GetSummaryForBookingAsync(long bookingId, CancellationToken ct);
    Task<DepositRefundSummaryDto?> GetSummaryForInvoiceAsync(long invoiceId, CancellationToken ct);
    Task<DepositRefundSummaryDto?> GetSummaryForSessionAsync(long sessionId, CancellationToken ct);
    Task<PublicDepositRefundDto> GetPublicAsync(string token, CancellationToken ct);
    Task SendVerificationCodeAsync(string token, CancellationToken ct);
    Task<PublicDepositRefundDto> VerifyCustomerAsync(string token, VerifyDepositRefundRequest request, CancellationToken ct);
    Task<PublicDepositRefundDto> SubmitMethodAsync(string token, SubmitDepositRefundMethodRequest request, CancellationToken ct);
    Task<PagedResult<DepositRefundManagementDto>> GetRefundsAsync(DepositRefundQueryRequest request, CancellationToken ct);
    Task<DepositRefundManagementDto> GetManagementAsync(long refundId, CancellationToken ct);
    Task<DepositRefundBankInfoDto> GetBankInfoAsync(long refundId, long actorUserId, CancellationToken ct);
    Task<BookingDepositRefundDto> RequestCustomerUpdateAsync(long refundId, long actorUserId, string? reason, CancellationToken ct);
    Task<BookingDepositRefundDto> PrepareCashPickupAsync(long refundId, long processedByUserId, CancellationToken ct);
    Task<BookingDepositRefundDto> CompleteBankTransferAsync(long refundId, long processedByUserId, CompleteBankTransferRefundRequest request, CancellationToken ct);
    Task<BookingDepositRefundDto> CompleteCashPickupAsync(long refundId, long processedByUserId, CompleteCashPickupRefundRequest request, CancellationToken ct);
}
