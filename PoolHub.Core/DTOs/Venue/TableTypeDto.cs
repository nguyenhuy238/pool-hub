namespace PoolHub.Core.DTOs.Venue;

public class TableTypeDto
{
    public long TableTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DefaultCapacity { get; set; }
}
