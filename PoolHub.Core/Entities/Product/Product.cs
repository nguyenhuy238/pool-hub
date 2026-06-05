namespace PoolHub.Core.Entities;

public class Product : BaseEntity
{
    public long ProductId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProductCategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool IsStockTracked { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}
