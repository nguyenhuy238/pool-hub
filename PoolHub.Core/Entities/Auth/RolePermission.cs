namespace PoolHub.Core.Entities;

public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public long? AssignedByUserId { get; set; }
}
