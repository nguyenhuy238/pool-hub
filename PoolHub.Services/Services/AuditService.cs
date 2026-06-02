using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.AuditLog;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;

namespace PoolHub.Services.Services;

public class AuditService(PoolHubDbContext db) : IAuditService
{
    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterRequest request, CancellationToken ct)
    {
        var query = db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(x => x.EntityName == request.EntityName);
        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId.Value);
        if (request.DateFrom.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= request.DateTo.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => x.Action.Contains(request.Search) || x.EntityName.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.AuditLogId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AuditLogDto
            {
                AuditLogId = x.AuditLogId,
                UserId = x.UserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Metadata = x.Metadata,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
        return new PagedResult<AuditLogDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task LogAsync(int? userId, string action, string entityName, string entityId, string? metadata, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Metadata = metadata
        });
        await db.SaveChangesAsync(ct);
    }
}
