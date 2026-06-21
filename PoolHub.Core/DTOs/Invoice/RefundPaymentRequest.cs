using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Invoice;

public class RefundPaymentRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}
