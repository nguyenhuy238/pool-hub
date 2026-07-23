using Microsoft.AspNetCore.Mvc;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;

namespace PoolHub.API.Controllers;

[ApiController]
public class HealthController(PoolHubDbContext db) : ControllerBase
{
    [HttpGet("health")]
    public async Task<ActionResult<ApiResponse<object>>> Get(CancellationToken ct)
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        return Ok(ApiResponse<object>.Ok(new
        {
            status = canConnect ? "Healthy" : "Degraded",
            database = canConnect,
            checkedAtUtc = DateTime.UtcNow
        }));
    }
}
