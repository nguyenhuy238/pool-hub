namespace PoolHub.Core.DTOs.Media;

public class MediaAssetDto
{
    public long MediaAssetId { get; set; }
    public Guid PublicId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? AltText { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class MediaQueryRequest
{
    public string? Keyword { get; set; }
    public string? MediaType { get; set; }
    public string? Folder { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MediaUploadInput
{
    public required Stream Content { get; set; }
    public required string OriginalFileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string Folder { get; set; } = "landing";
    public string? AltText { get; set; }
}
