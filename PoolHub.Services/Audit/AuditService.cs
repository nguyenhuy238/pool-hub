using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PoolHub.Core.DTOs.Audit;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Audit;

public class AuditService(PoolHubDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditService
{
    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (request.ActorUserId.HasValue) query = query.Where(x => x.ActorUserId == request.ActorUserId);
        if (!string.IsNullOrWhiteSpace(request.Action)) query = query.Where(x => x.Action.Contains(request.Action));
        if (!string.IsNullOrWhiteSpace(request.EntityName)) query = query.Where(x => x.EntityName.Contains(request.EntityName));
        if (request.FromDate.HasValue) query = query.Where(x => x.CreatedAtUtc >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(x => x.CreatedAtUtc <= request.ToDate.Value);
        var total = await query.CountAsync(ct);
        var items = await Project(query).OrderByDescending(x => x.AuditLogId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        return new PagedResult<AuditLogDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalItems = total };
    }

    public async Task<AuditLogDto> GetAuditLogAsync(long id, CancellationToken ct) =>
        await Project(db.AuditLogs.AsNoTracking().Where(x => x.AuditLogId == id)).FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("Audit log not found.");

    public async Task LogAsync(
        long? actorUserId,
        string action,
        string entityName,
        long? entityId = null,
        Guid? entityPublicId = null,
        object? oldValues = null,
        object? newValues = null,
        string? description = null,
        CancellationToken ct = default)
    {
        var context = httpContextAccessor.HttpContext;
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            EntityPublicId = entityPublicId,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
            Description = description
        });
        await db.SaveChangesAsync(ct);
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value);

    private IQueryable<AuditLogDto> Project(IQueryable<AuditLog> query) =>
        from log in query
        join actor in db.Users.AsNoTracking() on log.ActorUserId equals actor.UserId into actors
        from actor in actors.DefaultIfEmpty()
        select new AuditLogDto
        {
            AuditLogId = log.AuditLogId,
            ActorUserId = log.ActorUserId,
            ActorName = actor == null ? null : actor.FullName,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            EntityPublicId = log.EntityPublicId,
            OldValues = log.OldValues,
            NewValues = log.NewValues,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Description = log.Description,
            CreatedAtUtc = log.CreatedAtUtc
        };
}
