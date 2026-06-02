namespace PoolHub.Core.DTOs.Session;

public class SessionDto { public long SessionId { get; set; } public string SessionCode { get; set; } = string.Empty; public int Status { get; set; } public DateTime StartedAtUtc { get; set; } public DateTime? EndedAtUtc { get; set; } }
public class StartSessionRequest { public long TableId { get; set; } public long? BookingId { get; set; } public long? CustomerId { get; set; } }
public class TransferTableRequest { public long NewTableId { get; set; } }
