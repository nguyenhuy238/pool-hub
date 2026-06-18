using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");
        builder.HasKey(x => x.MediaAssetId);
        builder.HasIndex(x => x.PublicId).IsUnique();
        builder.HasIndex(x => x.Url).IsUnique();
        builder.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Folder).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MediaType).HasMaxLength(20).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
