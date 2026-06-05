using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.ToTable("discounts");
        builder.HasKey(x => x.DiscountId);
        builder.HasIndex(x => x.DiscountCode).IsUnique();
        builder.Property(x => x.Value).HasPrecision(19, 4);
        builder.Property(x => x.MaxAmount).HasPrecision(19, 4);
        builder.Property(x => x.MinTimeSubtotal).HasPrecision(19, 4);
    }
}
