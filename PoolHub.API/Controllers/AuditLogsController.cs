using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.AuditLog;
using PoolHub.Core.Interfaces;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager)]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] AuditLogFilterRequest request, CancellationToken ct)
        => Ok(ApiResponse<object>.Ok(await auditService.GetAuditLogsAsync(request, ct)));
}
