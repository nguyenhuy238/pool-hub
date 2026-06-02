namespace PoolHub.Core.Entities;

public class Session : BaseEntity
{
    public int SessionId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string SessionCode { get; set; } = string.Empty;
    public int? BookingId { get; set; }
    public int StartedByUserId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public int Status { get; set; } = 1;
}

public class SessionTableAssignment : BaseEntity
{
    public int AssignmentId { get; set; }
    public int SessionId { get; set; }
    public int TableId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
    public bool IsPrimary { get; set; }
}
