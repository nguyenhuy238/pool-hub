namespace PoolHub.Core.Entities;

public class SiteSetting : BaseEntity
{
    public long SiteSettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValueJson { get; set; } = "{}";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public long? UpdatedByUserId { get; set; }
}
