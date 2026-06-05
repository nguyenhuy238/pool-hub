namespace PoolHub.Core.Entities;

public class TableType : BaseEntity
{
    public long TableTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; } = true;
}
