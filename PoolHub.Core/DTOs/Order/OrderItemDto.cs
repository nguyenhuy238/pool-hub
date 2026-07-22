namespace PoolHub.Core.DTOs.Order;

public class OrderItemDto
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string? Note { get; set; }
}
