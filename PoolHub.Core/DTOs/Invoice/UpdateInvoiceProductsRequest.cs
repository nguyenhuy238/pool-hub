using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Invoice;

public class UpdateInvoiceProductsRequest
{
    [Required]
    public List<UpdateInvoiceProductItem> Products { get; set; } = new();
}

public class UpdateInvoiceProductItem
{
    [Required]
    public long ProductId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative.")]
    public int Quantity { get; set; }
}
