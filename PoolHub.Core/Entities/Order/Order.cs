namespace PoolHub.Core.Entities;

public class Order : BaseEntity
{
    public long OrderId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string OrderCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long OrderedByUserId { get; set; }
    public int Status { get; set; } = 1;
    public decimal SubtotalAmount { get; set; }
    public string? Note { get; set; }
}
