using System;

namespace PoolHub.Core.DTOs.Invoice;

public class PaymentDto
{
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public long PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentStatus { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public long? ReceivedByUserId { get; set; }
    public string? Note { get; set; }
}
