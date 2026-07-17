namespace PoolHub.Core.Entities;

public class VenueTable : BaseEntity
{
    public long TableId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ZoneId { get; set; }
    public long TableTypeId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public int OperationalStatus { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
