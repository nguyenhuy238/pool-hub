using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Invoice;

public class CancelInvoiceRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
