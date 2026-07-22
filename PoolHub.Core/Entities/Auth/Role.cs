namespace PoolHub.Core.Entities;

public class Role : BaseEntity
{
    public long RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
