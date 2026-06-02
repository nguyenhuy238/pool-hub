using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface IAuditService
{
    Task<PagedResult<object>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct);
}
