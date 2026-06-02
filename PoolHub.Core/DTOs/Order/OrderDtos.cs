namespace PoolHub.Core.DTOs.Order;

public class OrderDto { public long OrderId { get; set; } public long SessionId { get; set; } public int Status { get; set; } }
public class CreateOrderRequest { public long SessionId { get; set; } }
public class AddOrderItemRequest { public long ProductId { get; set; } public int Quantity { get; set; } }
