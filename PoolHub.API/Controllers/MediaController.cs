using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Media;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Extensions;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/admin/media")]
[Authorize(Roles = RoleConstants.Admin)]
public class MediaController(IMediaService service, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(31_457_280)]
    [RequestFormLimits(MultipartBodyLengthLimit = 31_457_280)]
    public async Task<ActionResult<ApiResponse<MediaAssetDto>>> Upload([FromForm] MediaUploadForm request, CancellationToken ct)
    {
        if (request.File is null) return BadRequest(ApiResponse<MediaAssetDto>.Fail("File is required."));
        var webRootPath = EnsureWebRoot();
        await using var content = request.File.OpenReadStream();
        var result = await service.UploadAsync(new MediaUploadInput
        {
            Content = content,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            SizeBytes = request.File.Length,
            Folder = request.Folder ?? "landing",
            AltText = request.AltText
        }, webRootPath, User.GetUserId(), ct);

        return Ok(ApiResponse<MediaAssetDto>.Ok(result, "Upload file successfully"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MediaAssetDto>>>> Get([FromQuery] MediaQueryRequest request, CancellationToken ct)
        => Ok(ApiResponse<PagedResult<MediaAssetDto>>.Ok(await service.GetAsync(request, ct)));

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, EnsureWebRoot(), User.GetUserId(), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Media deleted"));
    }

    private string EnsureWebRoot()
    {
        var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(webRootPath);
        return webRootPath;
    }
}

public class MediaUploadForm
{
    public IFormFile? File { get; set; }
    public string? Folder { get; set; }
    public string? AltText { get; set; }
}
