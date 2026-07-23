namespace PoolHub.Core.Entities;

public class BookingDeposit : BaseEntity
{
    public long BookingDepositId { get; set; }
    public long BookingId { get; set; }
    public decimal RequiredAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AppliedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal ForfeitedAmount { get; set; }
    public int Status { get; set; }
    public long? PaymentMethodId { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public long? AppliedToInvoiceId { get; set; }
    public DateTime? RefundedAtUtc { get; set; }
    public DateTime? ForfeitedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public ICollection<BookingDepositRefund> Refunds { get; set; } = new List<BookingDepositRefund>();
}
