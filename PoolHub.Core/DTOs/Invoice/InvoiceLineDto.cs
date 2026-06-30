namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceLineDto
{
    public long InvoiceLineId { get; set; }
    public long InvoiceId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotalAmount { get; set; }
    public long? ProductId { get; set; }
}
