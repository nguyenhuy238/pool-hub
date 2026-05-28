namespace PoolHub.Core.DTOs.Session;

public class SessionDto { public int SessionId { get; set; } public string SessionCode { get; set; } = string.Empty; public int Status { get; set; } public DateTime StartTimeUtc { get; set; } public DateTime? EndTimeUtc { get; set; } }
public class StartSessionRequest { public int TableId { get; set; } public int? BookingId { get; set; } }
public class TransferTableRequest { public int NewTableId { get; set; } }
