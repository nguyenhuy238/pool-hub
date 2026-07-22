namespace PoolHub.Core.Entities;

public class BookingTable : BaseEntity
{
    public long BookingTableId { get; set; }
    public long BookingId { get; set; }
    public long TableId { get; set; }
}
