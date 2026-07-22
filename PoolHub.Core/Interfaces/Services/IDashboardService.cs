using PoolHub.Core.DTOs.Dashboard;

namespace PoolHub.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(long? userId, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions, CancellationToken ct);
    Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken ct);
    Task<List<RevenuePointDto>> GetRevenueAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct);
    Task<List<ActiveSessionDashboardDto>> GetActiveSessionsAsync(CancellationToken ct);
    Task<List<LowStockProductDto>> GetLowStockProductsAsync(CancellationToken ct);
    Task<List<RecentAuditLogDto>> GetRecentAuditLogsAsync(CancellationToken ct);
}
