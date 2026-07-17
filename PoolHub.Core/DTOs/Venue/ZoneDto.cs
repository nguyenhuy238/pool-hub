namespace PoolHub.Core.DTOs.Venue;

public class ZoneDto
{
    public long ZoneId { get; set; }
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
