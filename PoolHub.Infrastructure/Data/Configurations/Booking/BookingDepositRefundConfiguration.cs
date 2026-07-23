using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class BookingDepositRefundConfiguration : IEntityTypeConfiguration<BookingDepositRefund>
{
    public void Configure(EntityTypeBuilder<BookingDepositRefund> builder)
    {
        builder.ToTable("booking_deposit_refunds", table =>
            table.HasCheckConstraint("ck_booking_deposit_refunds_amount_positive", "amount > 0"));
        builder.HasKey(x => x.BookingDepositRefundId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.RefundCode).IsUnique();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.BookingDepositId);
        builder.HasIndex(x => x.CreatedAtUtc);

        builder.Property(x => x.RefundCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(19, 4);
        builder.Property(x => x.RowVersion).IsRowVersion().HasColumnName("row_version");
        builder.Property(x => x.CustomerEmailSnapshot).HasMaxLength(256);
        builder.Property(x => x.CustomerPhoneSnapshot).HasMaxLength(32);
        builder.Property(x => x.CustomerTokenHash).HasMaxLength(128);
        builder.Property(x => x.CustomerBankCode).HasMaxLength(32);
        builder.Property(x => x.CustomerBankName).HasMaxLength(128);
        builder.Property(x => x.CustomerBankAccountLast4).HasMaxLength(4);
        builder.Property(x => x.ManualTransferCode).HasMaxLength(128);
        builder.Property(x => x.CashReceiptCode).HasMaxLength(128);

        builder.HasOne<BookingDeposit>()
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.BookingDepositId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Booking>().WithMany().HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<MediaAsset>().WithMany().HasForeignKey(x => x.ProofMediaAssetId).OnDelete(DeleteBehavior.NoAction);
    }
}
