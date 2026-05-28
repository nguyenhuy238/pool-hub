using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Product;

public class ProductCategoryDto { public int CategoryId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
public class ProductDto { public int ProductId { get; set; } public int CategoryId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public decimal UnitPrice { get; set; } public int StockQuantity { get; set; } }
public class CreateProductRequest { [Required] public int CategoryId { get; set; } [Required] public string Name { get; set; } = string.Empty; [Required] public string Code { get; set; } = string.Empty; [Range(0.01, 999999)] public decimal UnitPrice { get; set; } [Range(0, 99999)] public int StockQuantity { get; set; } }
