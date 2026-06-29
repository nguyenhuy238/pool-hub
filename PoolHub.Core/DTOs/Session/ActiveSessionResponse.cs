namespace PoolHub.Core.DTOs.Session;

public class ActiveSessionResponse
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public long? CustomerId { get; set; }
    public long? BookingId { get; set; }
    public ActiveSessionTableDto? CurrentTable { get; set; }
}

public class ActiveSessionTableDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long ZoneId { get; set; }
    public long FloorId { get; set; }
}
