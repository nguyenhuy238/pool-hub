namespace PoolHub.Core.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
