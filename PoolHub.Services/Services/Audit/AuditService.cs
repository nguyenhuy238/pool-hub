using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;

namespace PoolHub.Services.Services.Audit;

public class AuditService(IInvoiceRepository repo) : IAuditService // reusing repo since it exposes GetAuditLogs()
{
    public async Task<PagedResult<object>> GetAuditLogsAsync(PaginationRequest request, CancellationToken ct)
    {
        var query = repo.GetAuditLogs();
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.AuditLogId).Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).Select(x => (object)new { x.AuditLogId, x.UserId, x.Action, x.EntityName, x.EntityId, x.CreatedAtUtc }).ToListAsync(ct);
        return new PagedResult<object> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }
}
