using System.ComponentModel.DataAnnotations;
using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Admin;

public class DiscountQueryRequest : PaginationRequest
{
    public bool? IsActive { get; set; }
    public string? DiscountType { get; set; }
    public string? AppliesTo { get; set; }
    public bool? IsVoucher { get; set; }
    public long? CustomerId { get; set; }
    public bool? OnlyTemplates { get; set; }
}

public class DiscountDto
{
    public long DiscountId { get; set; }
    public string DiscountCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinTimeSubtotal { get; set; }
    public string AppliesTo { get; set; } = "TIME";
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; }
    public bool IsVoucher { get; set; }
    public int? PointsRequired { get; set; }
    public long? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int MaxUsage { get; set; }
    public int UsageCount { get; set; }
}

public class UpsertDiscountRequest
{
    [Required, MaxLength(50)] public string DiscountCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public string DiscountType { get; set; } = "PERCENTAGE";
    [Range(0.0001, double.MaxValue)] public decimal Value { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinTimeSubtotal { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVoucher { get; set; } = false;
    public int? PointsRequired { get; set; }
    public long? CustomerId { get; set; }
    public int MaxUsage { get; set; } = 0;
}

public class UpdateActiveStatusRequest
{
    public bool IsActive { get; set; }
}

public class ValidateDiscountRequest
{
    [Required] public string DiscountCode { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal TimeSubtotal { get; set; }
}

public class DiscountValidationDto
{
    public bool IsValid { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class InventoryQueryRequest : PaginationRequest
{
    public long? ProductId { get; set; }
    public int? TransactionType { get; set; }
    public string? ReferenceType { get; set; }
    public long? InvoiceId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public class InventoryTransactionDto
{
    public long InventoryTransactionId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TransactionType { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public long? OrderId { get; set; }
    public long? InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public string? Note { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class StockAdjustRequest
{
    public long ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [Range(1, 3)] public int TransactionType { get; set; }
    public decimal? UnitCost { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}

public class LowStockProductDto
{
    public long ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
}

public class PaymentQueryRequest : PaginationRequest
{
    public long? InvoiceId { get; set; }
    public int? PaymentStatus { get; set; }
    public DateTime? Date { get; set; }
}

public class UpsertPaymentMethodRequest
{
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReportQueryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class RevenueReportDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class TableUsageReportDto
{
    public long TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public int TotalMinutes { get; set; }
}

public class ProductSalesReportDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public class BookingReportDto
{
    public int Status { get; set; }
    public int Count { get; set; }
}

public class CustomerReportDto
{
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public int SessionCount { get; set; }
    public decimal Revenue { get; set; }
}

public class PaymentMethodReportDto
{
    public long PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public int PaymentCount { get; set; }
    public decimal Amount { get; set; }
}

public class InventoryReportDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int NetMovement { get; set; }
    public decimal InventoryValue { get; set; }
}
