namespace PoolHub.Core.Entities;

public class Order : BaseEntity
{
    public int OrderId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int SessionId { get; set; }
    public int CreatedByUserId { get; set; }
    public int Status { get; set; } = 1;
}

public class OrderItem : BaseEntity
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
