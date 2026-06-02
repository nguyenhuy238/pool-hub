using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Product;

public class ProductCategoryDto { public int CategoryId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public string? Description { get; set; } public bool IsActive { get; set; } = true; }
public class ProductDto { public int ProductId { get; set; } public int CategoryId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public decimal UnitPrice { get; set; } public int StockQuantity { get; set; } public bool IsActive { get; set; } = true; }
public class CreateProductRequest { [Required] public int CategoryId { get; set; } [Required] public string Name { get; set; } = string.Empty; [Required] public string Code { get; set; } = string.Empty; [Range(0.01, 999999)] public decimal UnitPrice { get; set; } [Range(0, 99999)] public int StockQuantity { get; set; } }
public class UpdateProductRequest { public int CategoryId { get; set; } [Required] public string Name { get; set; } = string.Empty; [Range(0.01, 999999)] public decimal UnitPrice { get; set; } }
public class StockAdjustRequest { [Required] public int QuantityChange { get; set; } [Required] public string Reason { get; set; } = "IMPORT"; }
public class InventoryTransactionDto { public int InventoryTransactionId { get; set; } public int ProductId { get; set; } public int QuantityChange { get; set; } public int StockAfter { get; set; } public string Reason { get; set; } = string.Empty; public int? CreatedByUserId { get; set; } public DateTime CreatedAtUtc { get; set; } }
