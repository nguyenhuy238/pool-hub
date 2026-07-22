using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class BookingTableConfiguration : IEntityTypeConfiguration<BookingTable>
{
    public void Configure(EntityTypeBuilder<BookingTable> builder)
    {
        builder.ToTable("booking_tables");
        builder.HasKey(x => x.BookingTableId);
        builder.HasIndex(x => new { x.BookingId, x.TableId }).IsUnique();
        builder.HasOne<Booking>().WithMany(x => x.BookingTables).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<VenueTable>().WithMany().HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.Restrict);
    }
}
