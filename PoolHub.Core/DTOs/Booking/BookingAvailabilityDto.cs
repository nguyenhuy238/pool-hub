namespace PoolHub.Core.DTOs.Booking;

public class BookingAvailabilityRequest
{
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public long? TableId { get; set; }
    public List<long>? TableIds { get; set; }
    public long? TableTypeId { get; set; }
}

public class AvailableTableDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public long TableTypeId { get; set; }
    public string TableTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal DepositRequiredAmount { get; set; }
    public int DepositPercent { get; set; }
    public bool RequiresApproval { get; set; }
    public List<string> ConflictDetails { get; set; } = [];
}
