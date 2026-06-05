using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Product;

public class CreateProductRequest
{
    [Required]
    public long ProductCategoryId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Sku { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, 99999)]
    public int StockQuantity { get; set; }
}
