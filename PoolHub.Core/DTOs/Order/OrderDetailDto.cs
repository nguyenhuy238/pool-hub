using System;
using System.Collections.Generic;

namespace PoolHub.Core.DTOs.Order;

public class OrderDetailDto
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long OrderedByUserId { get; set; }
    public int Status { get; set; }
    public decimal SubtotalAmount { get; set; }
    public string? Note { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}
