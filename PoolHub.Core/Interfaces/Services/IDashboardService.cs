using PoolHub.Core.DTOs.Dashboard;

namespace PoolHub.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(long? userId, CancellationToken ct);
}
