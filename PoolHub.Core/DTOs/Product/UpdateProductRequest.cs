using System;
using System.Collections.Generic;
using System.Text;

namespace PoolHub.Core.DTOs.Product
{
    public class UpdateProductRequest
    {
        public int ProductCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
    }
}
