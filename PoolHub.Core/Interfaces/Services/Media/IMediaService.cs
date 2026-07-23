using PoolHub.Core.DTOs.Media;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IMediaService
{
    Task<MediaAssetDto> UploadAsync(MediaUploadInput input, string webRootPath, long userId, CancellationToken ct);
    Task<PagedResult<MediaAssetDto>> GetAsync(MediaQueryRequest request, CancellationToken ct);
    Task DeleteAsync(long id, string webRootPath, long userId, CancellationToken ct);
}
