namespace PoolHub.Core.Entities;

public class BookingDepositRefund : BaseEntity
{
    public long BookingDepositRefundId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
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
    public string? CustomerEmailSnapshot { get; set; }
    public string? CustomerPhoneSnapshot { get; set; }
    public string? CustomerTokenHash { get; set; }
    public DateTime? CustomerTokenExpiresAtUtc { get; set; }
    public DateTime? CustomerTokenGeneratedAtUtc { get; set; }
    public string? VerificationCodeHash { get; set; }
    public DateTime? VerificationCodeExpiresAtUtc { get; set; }
    public DateTime? VerificationCodeSentAtUtc { get; set; }
    public int VerificationFailedAttempts { get; set; }
    public DateTime? CustomerVerifiedAtUtc { get; set; }
    public DateTime? CustomerInfoSubmittedAtUtc { get; set; }
    public string? CustomerBankCode { get; set; }
    public string? CustomerBankName { get; set; }
    public string? CustomerBankAccountNumberEncrypted { get; set; }
    public string? CustomerBankAccountLast4 { get; set; }
    public string? CustomerBankAccountNameEncrypted { get; set; }
    public string? ManualTransferCode { get; set; }
    public string? CashReceiptCode { get; set; }
    public string? CashPickupCodeHash { get; set; }
    public DateTime? CashPickupCodeExpiresAtUtc { get; set; }
    public DateTime? CashPickupCodeUsedAtUtc { get; set; }
    public long? ProofMediaAssetId { get; set; }
    public string? Note { get; set; }
    public long? RequestedByUserId { get; set; }
    public long? ApprovedByUserId { get; set; }
    public long? ProcessedByUserId { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? ProcessingAtUtc { get; set; }
    public DateTime? SucceededAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime? RejectedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string? RejectReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
