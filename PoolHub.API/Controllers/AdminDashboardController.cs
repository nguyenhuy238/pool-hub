using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PoolHub.Core.DTOs.Dashboard;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Constants;

namespace PoolHub.API.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = PermissionConstants.ReportsView)]
public class AdminDashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<AdminDashboardSummaryDto>>> Summary(CancellationToken ct) =>
        Ok(ApiResponse<AdminDashboardSummaryDto>.Ok(await dashboardService.GetAdminSummaryAsync(ct)));

    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<List<RevenuePointDto>>>> Revenue([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct) =>
        Ok(ApiResponse<List<RevenuePointDto>>.Ok(await dashboardService.GetRevenueAsync(fromDate, toDate, ct)));

    [HttpGet("active-sessions")]
    public async Task<ActionResult<ApiResponse<List<ActiveSessionDashboardDto>>>> ActiveSessions(CancellationToken ct) =>
        Ok(ApiResponse<List<ActiveSessionDashboardDto>>.Ok(await dashboardService.GetActiveSessionsAsync(ct)));

    [HttpGet("low-stock-products")]
    public async Task<ActionResult<ApiResponse<List<LowStockProductDto>>>> LowStockProducts(CancellationToken ct) =>
        Ok(ApiResponse<List<LowStockProductDto>>.Ok(await dashboardService.GetLowStockProductsAsync(ct)));

    [HttpGet("recent-audit-logs")]
    public async Task<ActionResult<ApiResponse<List<RecentAuditLogDto>>>> RecentAuditLogs(CancellationToken ct) =>
        Ok(ApiResponse<List<RecentAuditLogDto>>.Ok(await dashboardService.GetRecentAuditLogsAsync(ct)));
}
