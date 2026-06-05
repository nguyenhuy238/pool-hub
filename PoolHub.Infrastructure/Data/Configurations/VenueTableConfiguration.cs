using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class VenueTableConfiguration : IEntityTypeConfiguration<VenueTable>
{
    public void Configure(EntityTypeBuilder<VenueTable> builder)
    {
        builder.ToTable("venue_tables");
        builder.HasKey(x => x.TableId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.TableCode).IsUnique();
        builder.Property(x => x.PositionX).HasPrecision(10, 2);
        builder.Property(x => x.PositionY).HasPrecision(10, 2);
        builder.HasOne<Zone>().WithMany().HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableType>().WithMany().HasForeignKey(x => x.TableTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
