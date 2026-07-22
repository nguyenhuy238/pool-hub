namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceDto
{
    public long InvoiceId { get; set; }
    public long SessionId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public decimal GrandTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int PaymentStatus { get; set; }
    public int Status { get; set; }
    public decimal DepositAppliedAmount { get; set; }
    public decimal DepositRefundAmount { get; set; }
    public decimal RemainingAmount { get; set; }
}
