using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Media;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Media;

public partial class MediaService(PoolHubDbContext db) : IMediaService
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxVideoBytes = 30 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ["image/jpeg"],
        [".jpeg"] = ["image/jpeg"],
        [".png"] = ["image/png"],
        [".webp"] = ["image/webp"],
        [".gif"] = ["image/gif"],
        [".ico"] = ["image/x-icon", "image/vnd.microsoft.icon", "application/octet-stream"],
        [".mp4"] = ["video/mp4"],
        [".webm"] = ["video/webm"]
    };

    public async Task<MediaAssetDto> UploadAsync(MediaUploadInput input, string webRootPath, long userId, CancellationToken ct)
    {
        if (userId <= 0) throw new UnauthorizedException("Authenticated admin is required.");
        if (input.SizeBytes <= 0) throw new ValidationException("Uploaded file is empty.");

        var extension = Path.GetExtension(input.OriginalFileName).ToLowerInvariant();
        if (!AllowedFiles.TryGetValue(extension, out var allowedContentTypes))
        {
            throw new ValidationException("Unsupported file extension.");
        }

        if (!allowedContentTypes.Contains(input.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("File content type does not match its extension.");
        }

        var mediaType = extension is ".mp4" or ".webm" ? "Video" : "Image";
        var maxBytes = mediaType == "Video" ? MaxVideoBytes : MaxImageBytes;
        if (input.SizeBytes > maxBytes)
        {
            throw new ValidationException(mediaType == "Video" ? "Video must not exceed 30MB." : "Image must not exceed 5MB.");
        }
        await ValidateSignatureAsync(input.Content, extension, ct);

        var folder = NormalizeFolder(input.Folder);
        var storedFileName = $"{folder}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..(folder.Length + 1 + 14 + 1 + 6)] + extension;
        var uploadRoot = ResolveUploadRoot(webRootPath);
        var folderPath = Path.GetFullPath(Path.Combine(uploadRoot, folder));
        EnsureInside(uploadRoot, folderPath);
        Directory.CreateDirectory(folderPath);

        var physicalPath = Path.GetFullPath(Path.Combine(folderPath, storedFileName));
        EnsureInside(uploadRoot, physicalPath);

        await using (var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
        {
            await input.Content.CopyToAsync(output, ct);
        }

        var asset = new MediaAsset
        {
            OriginalFileName = Path.GetFileName(input.OriginalFileName),
            StoredFileName = storedFileName,
            Url = $"/uploads/{folder}/{storedFileName}",
            Folder = folder,
            ContentType = input.ContentType,
            MediaType = mediaType,
            SizeBytes = input.SizeBytes,
            AltText = input.AltText?.Trim(),
            UploadedByUserId = userId,
            IsActive = true
        };

        db.MediaAssets.Add(asset);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "UPLOAD_MEDIA",
            EntityName = "MediaAsset",
            EntityPublicId = asset.PublicId,
            NewValues = asset.Url,
            Description = "Admin uploaded media file"
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            if (File.Exists(physicalPath)) File.Delete(physicalPath);
            throw;
        }

        return ToDto(asset);
    }

    public async Task<PagedResult<MediaAssetDto>> GetAsync(MediaQueryRequest request, CancellationToken ct)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.MediaAssets.AsNoTracking().Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(x => x.OriginalFileName.Contains(keyword) || x.StoredFileName.Contains(keyword) || (x.AltText != null && x.AltText.Contains(keyword)));
        }
        if (!string.IsNullOrWhiteSpace(request.MediaType) && !request.MediaType.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var mediaType = request.MediaType.Trim();
            query = query.Where(x => x.MediaType == mediaType);
        }
        if (!string.IsNullOrWhiteSpace(request.Folder) && !request.Folder.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var folder = request.Folder.Trim();
            query = query.Where(x => x.Folder == folder);
        }

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MediaAssetDto
            {
                MediaAssetId = x.MediaAssetId,
                PublicId = x.PublicId,
                Url = x.Url,
                FileName = x.StoredFileName,
                OriginalFileName = x.OriginalFileName,
                Folder = x.Folder,
                ContentType = x.ContentType,
                MediaType = x.MediaType,
                SizeBytes = x.SizeBytes,
                AltText = x.AltText,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync(ct);

        return new PagedResult<MediaAssetDto> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalItems = total };
    }

    public async Task DeleteAsync(long id, string webRootPath, long userId, CancellationToken ct)
    {
        var asset = await db.MediaAssets.FirstOrDefaultAsync(x => x.MediaAssetId == id && x.IsActive, ct)
            ?? throw new NotFoundException("Media asset was not found.");

        if (await db.SiteSettings.AnyAsync(x => x.IsActive && x.SettingValueJson.Contains(asset.Url), ct))
        {
            throw new ConflictException("Media is currently used by Landing Page Settings.");
        }

        asset.IsActive = false;
        asset.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "DELETE_MEDIA",
            EntityName = "MediaAsset",
            EntityId = asset.MediaAssetId,
            EntityPublicId = asset.PublicId,
            OldValues = asset.Url,
            Description = "Admin deleted media file"
        });
        await db.SaveChangesAsync(ct);

        var uploadRoot = ResolveUploadRoot(webRootPath);
        var physicalPath = Path.GetFullPath(Path.Combine(webRootPath, asset.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        EnsureInside(uploadRoot, physicalPath);
        try
        {
            if (File.Exists(physicalPath)) File.Delete(physicalPath);
        }
        catch (IOException)
        {
            // The database record remains inactive; an operations cleanup can remove a locked orphan file later.
        }
    }

    private static MediaAssetDto ToDto(MediaAsset asset) => new()
    {
        MediaAssetId = asset.MediaAssetId,
        PublicId = asset.PublicId,
        Url = asset.Url,
        FileName = asset.StoredFileName,
        OriginalFileName = asset.OriginalFileName,
        Folder = asset.Folder,
        ContentType = asset.ContentType,
        MediaType = asset.MediaType,
        SizeBytes = asset.SizeBytes,
        AltText = asset.AltText,
        CreatedAtUtc = asset.CreatedAtUtc
    };

    private static async Task ValidateSignatureAsync(Stream content, string extension, CancellationToken ct)
    {
        if (!content.CanSeek) throw new ValidationException("Uploaded file stream cannot be validated.");
        var header = new byte[16];
        var read = await content.ReadAsync(header.AsMemory(0, header.Length), ct);
        content.Position = 0;

        var valid = extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => read >= 6 && (System.Text.Encoding.ASCII.GetString(header, 0, 6) is "GIF87a" or "GIF89a"),
            ".webp" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
            ".ico" => read >= 4 && header[0] == 0 && header[1] == 0 && header[2] == 1 && header[3] == 0,
            ".mp4" => read >= 12 && System.Text.Encoding.ASCII.GetString(header, 4, 4) == "ftyp",
            ".webm" => read >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            _ => false
        };

        if (!valid) throw new ValidationException("File signature does not match the selected format.");
    }

    private static string NormalizeFolder(string? folder)
    {
        var value = string.IsNullOrWhiteSpace(folder) ? "landing" : folder.Trim().ToLowerInvariant();
        if (!FolderPattern().IsMatch(value)) throw new ValidationException("Invalid media folder.");
        return value;
    }

    private static string ResolveUploadRoot(string webRootPath)
    {
        var root = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void EnsureInside(string root, string candidate)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Invalid media path.");
        }
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9_-]{0,39}$")]
    private static partial Regex FolderPattern();
}
