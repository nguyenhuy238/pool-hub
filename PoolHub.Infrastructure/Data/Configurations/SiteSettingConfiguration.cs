using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Data.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("site_settings");
        builder.HasKey(x => x.SiteSettingId);
        builder.Property(x => x.SettingKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.SettingValueJson).IsRequired();
        builder.HasIndex(x => x.SettingKey).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
