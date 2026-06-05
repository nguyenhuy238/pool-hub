namespace PoolHub.Core.DTOs.Venue;

public class VenueTableDto
{
    public long TableId { get; set; }
    public long ZoneId { get; set; }
    public long TableTypeId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int OperationalStatus { get; set; }
}
