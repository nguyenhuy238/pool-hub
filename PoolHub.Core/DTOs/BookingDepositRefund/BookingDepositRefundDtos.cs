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
