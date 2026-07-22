using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.OrderId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder.Property(x => x.SubtotalAmount).HasPrecision(19, 4);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OrderedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
