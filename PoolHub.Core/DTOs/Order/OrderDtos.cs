namespace PoolHub.Core.DTOs.Order;

public class OrderDto { public int OrderId { get; set; } public int SessionId { get; set; } public int Status { get; set; } }
public class CreateOrderRequest { public int SessionId { get; set; } }
public class AddOrderItemRequest { public int ProductId { get; set; } public int Quantity { get; set; } }
