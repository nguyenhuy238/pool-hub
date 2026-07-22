using System;

namespace PoolHub.Core.DTOs.Invoice;

public class PaymentDto
{
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public long PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public decimal Amount { get; set; }
    public int PaymentStatus { get; set; }
    public string? TransactionCode { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public long? ReceivedByUserId { get; set; }
    public string? Note { get; set; }

    private string? _refundReason;
    public string? RefundReason 
    { 
        get 
        {
            if (_refundReason != null) return _refundReason;
            if (PaymentStatus != 4) return null;
            if (string.IsNullOrEmpty(Note)) return null;
            var idx = Note.IndexOf("Refunded: ", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) return Note.Substring(idx + 10).Trim();
            return Note;
        }
        set => _refundReason = value;
    }
}
