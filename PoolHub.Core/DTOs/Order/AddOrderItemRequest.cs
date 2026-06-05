namespace PoolHub.Core.DTOs.Order;

public class AddOrderItemRequest
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}
