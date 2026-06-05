using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.ProductId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.UnitPrice).HasPrecision(19, 4);
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnName("row_version");
        builder.HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
