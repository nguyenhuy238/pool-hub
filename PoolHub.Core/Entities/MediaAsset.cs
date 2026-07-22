namespace PoolHub.Core.Entities;

public class MediaAsset : BaseEntity
{
    public long MediaAssetId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Folder { get; set; } = "landing";
    public string ContentType { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image";
    public long SizeBytes { get; set; }
    public string? AltText { get; set; }
    public long UploadedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
}
