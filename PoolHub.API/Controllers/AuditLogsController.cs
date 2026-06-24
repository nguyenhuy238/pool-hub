using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Audit;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Manager, Policy = PermissionConstants.AuditView)]
public class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> Get([FromQuery] AuditLogQueryRequest request, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await auditService.GetAuditLogsAsync(request, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(long id, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await auditService.GetAuditLogAsync(id, ct)));
}
