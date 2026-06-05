namespace PoolHub.Core.Entities;

public class Session : BaseEntity
{
    public long SessionId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string SessionCode { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public long? BookingId { get; set; }
    public int Status { get; set; } = 1;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public long OpenedByUserId { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? Note { get; set; }
}
