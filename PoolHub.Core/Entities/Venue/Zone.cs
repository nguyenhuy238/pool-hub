namespace PoolHub.Core.Entities;

public class Zone : BaseEntity
{
    public long ZoneId { get; set; }
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
