namespace PoolHub.Core.Entities;

public class ProductCategory : BaseEntity
{
    public long ProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
