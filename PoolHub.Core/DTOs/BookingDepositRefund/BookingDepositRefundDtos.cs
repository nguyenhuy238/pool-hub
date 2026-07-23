using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.BookingDepositRefund;

public class BookingDepositRefundDto
{
    public long BookingDepositRefundId { get; set; }
    public Guid PublicId { get; set; }
    public long BookingDepositId { get; set; }
    public long BookingId { get; set; }
    public long? InvoiceId { get; set; }
    public long? CustomerId { get; set; }
    public string RefundCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? RefundMethod { get; set; }
    public string? ReasonDetail { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreateBookingDepositRefundRequest
{
    public long BookingDepositId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ReasonDetail { get; set; }
    public long? InvoiceId { get; set; }
    public int? RefundMethod { get; set; }
    public long? RequestedByUserId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class DepositApplicationResult
{
    public decimal AppliedAmount { get; set; }
    public decimal ExcessAmount { get; set; }
    public bool PaymentCreated { get; set; }
    public BookingDepositRefundDto? ExcessRefund { get; set; }
}

public class DepositRefundSummaryDto
{
    public decimal PaidAmount { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal ForfeitedAmount { get; set; }
    public decimal PendingRefundAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RefundableBalance { get; set; }
    public List<DepositRefundRequestSummaryDto> RefundRequests { get; set; } = [];
}

public class DepositRefundRequestSummaryDto
{
    public long BookingDepositRefundId { get; set; }
    public Guid PublicId { get; set; }
    public string RefundCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? RefundMethod { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SucceededAtUtc { get; set; }
}

public class PublicDepositRefundDto
{
    public string RefundCode { get; set; } = string.Empty;
    public string? BookingCode { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? TokenExpiresAtUtc { get; set; }
    public string? CustomerEmailMasked { get; set; }
    public string? CustomerPhoneMasked { get; set; }
    public int? RefundMethod { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountLast4 { get; set; }
    public bool IsVerified { get; set; }
    public string NextStep { get; set; } = string.Empty;
}

public class VerifyDepositRefundRequest
{
    public string VerificationCode { get; set; } = string.Empty;
    public string PhoneLast4 { get; set; } = string.Empty;
}

public class SubmitDepositRefundMethodRequest
{
    public string RefundMethod { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? ConfirmAccountNumber { get; set; }
    public string? AccountHolderName { get; set; }
}

public class DepositRefundQueryRequest : PaginationRequest
{
    public int? Status { get; set; }
    public int? RefundMethod { get; set; }
    public string? Reason { get; set; }
    public string? BookingCode { get; set; }
    public string? RefundCode { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
}

public class DepositRefundManagementDto : BookingDepositRefundDto
{
    public string? BookingCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmailMasked { get; set; }
    public string? CustomerPhoneMasked { get; set; }
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountLast4 { get; set; }
    public string? ManualTransferCode { get; set; }
    public string? FailureReason { get; set; }
    public string? RejectReason { get; set; }
    public string? Note { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? ProcessingAtUtc { get; set; }
    public DateTime? SucceededAtUtc { get; set; }
}

public class DepositRefundBankInfoDto
{
    public long BookingDepositRefundId { get; set; }
    public string RefundCode { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountHolderName { get; set; }
    public string? AccountLast4 { get; set; }
}

public class RejectDepositRefundRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class RequestCustomerRefundUpdateRequest
{
    public string? Reason { get; set; }
}

public class CompleteBankTransferRefundRequest
{
    public string ManualTransferCode { get; set; } = string.Empty;
    public long? ProofMediaAssetId { get; set; }
    public string? Note { get; set; }
}

public class MarkDepositRefundFailedRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class CompleteCashPickupRefundRequest
{
    public string CashPickupCode { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public string PhoneLast4 { get; set; } = string.Empty;
    public string? Note { get; set; }
}
