namespace PoolHub.Core.DTOs.Product;

public class ProductDto
{
    public long ProductId { get; set; }
    public long ProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
}
