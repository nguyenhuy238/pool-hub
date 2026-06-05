using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;

namespace PoolHub.Services.Audit;

public class AuditService(PoolHubDbContext db) : IAuditService
{
    public async Task<PagedResult<object>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.AuditLogId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => (object)new { x.AuditLogId, x.ActorUserId, x.Action, x.EntityName, x.EntityId, x.EntityPublicId, x.CreatedAtUtc }).ToListAsync(ct);
        return new PagedResult<object> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }
}
