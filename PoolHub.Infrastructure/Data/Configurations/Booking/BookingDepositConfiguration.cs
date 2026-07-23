using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class BookingDepositConfiguration : IEntityTypeConfiguration<BookingDeposit>
{
    public void Configure(EntityTypeBuilder<BookingDeposit> builder)
    {
        builder.ToTable("booking_deposits");
        builder.HasKey(x => x.BookingDepositId);
        builder.HasIndex(x => x.BookingId).IsUnique();
        builder.Property(x => x.RequiredAmount).HasPrecision(19, 4);
        builder.Property(x => x.PaidAmount).HasPrecision(19, 4);
        builder.Property(x => x.AppliedAmount).HasPrecision(19, 4);
        builder.Property(x => x.RefundedAmount).HasPrecision(19, 4);
        builder.Property(x => x.ForfeitedAmount).HasPrecision(19, 4);
        builder.HasOne<Booking>().WithOne(x => x.Deposit).HasForeignKey<BookingDeposit>(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PaymentMethod>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.AppliedToInvoiceId).OnDelete(DeleteBehavior.NoAction);
    }
}
