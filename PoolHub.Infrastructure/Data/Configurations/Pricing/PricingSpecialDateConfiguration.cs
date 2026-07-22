using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class PricingSpecialDateConfiguration : IEntityTypeConfiguration<PricingSpecialDate>
{
    public void Configure(EntityTypeBuilder<PricingSpecialDate> builder)
    {
        builder.ToTable("pricing_special_dates");
        builder.HasKey(x => x.PricingSpecialDateId);
        
        // A date can only have one configuration
        builder.HasIndex(x => x.Date).IsUnique();
        
        builder.Property(x => x.Description).HasMaxLength(255);
    }
}
