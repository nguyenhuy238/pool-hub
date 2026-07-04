namespace PoolHub.Core.Entities;

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public long? AssignedByUserId { get; set; }
}
