namespace PoolHub.Core.Entities;

public class ProductCategory : BaseEntity
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Product : BaseEntity
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class InventoryTransaction : BaseEntity
{
    public int InventoryTransactionId { get; set; }
    public int ProductId { get; set; }
    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int? CreatedByUserId { get; set; }
}
