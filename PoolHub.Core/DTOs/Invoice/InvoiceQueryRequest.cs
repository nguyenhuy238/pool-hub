using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Invoice;

public class InvoiceQueryRequest : PaginationRequest
{
    public int? PaymentStatus { get; set; }
    public int? Status { get; set; }
    public DateTime? Date { get; set; }
}
