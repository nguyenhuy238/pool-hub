using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Common;

public partial class CrudService
{
    public async Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.PricingPlans.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<PricingPlanDto> GetPricingPlanAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingPlans.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlan not found.");
        return new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive };
    }

    public async Task<PricingPlanDto> CreatePricingPlanAsync(PricingPlanDto d, CancellationToken ct)
    {
        var x = new PoolHub.Core.Entities.PricingPlan { Name = d.Name, IsDefault = d.IsDefault, IsActive = d.IsActive };
        db.PricingPlans.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(x.PricingPlanId, ct);
    }

    public async Task<PricingPlanDto> UpdatePricingPlanAsync(long id, PricingPlanDto d, CancellationToken ct)
    {
        var x = await db.PricingPlans.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlan not found.");
        x.Name = d.Name;
        x.IsDefault = d.IsDefault;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(id, ct);
    }

    public async Task DeletePricingPlanAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingPlans.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlan not found.");
        db.PricingPlans.Remove(x);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.PricingPlanRules.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<PricingPlanRuleDto> GetPricingPlanRuleAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        return new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate };
    }

    public async Task<PricingPlanRuleDto> CreatePricingPlanRuleAsync(long planId, PricingPlanRuleDto d, CancellationToken ct)
    {
        var x = new PoolHub.Core.Entities.PricingPlanRule { PricingPlanId = planId, TableTypeId = d.TableTypeId, DayOfWeek = d.DayOfWeek, HourlyRate = d.HourlyRate };
        db.PricingPlanRules.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(x.PricingPlanRuleId, ct);
    }

    public async Task<PricingPlanRuleDto> UpdatePricingPlanRuleAsync(long planId, long ruleId, PricingPlanRuleDto d, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FirstOrDefaultAsync(r => r.PricingPlanId == planId && r.PricingPlanRuleId == ruleId, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        x.TableTypeId = d.TableTypeId;
        x.DayOfWeek = d.DayOfWeek;
        x.HourlyRate = d.HourlyRate;
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(ruleId, ct);
    }

    public async Task DeletePricingPlanRuleAsync(long planId, long ruleId, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FirstOrDefaultAsync(r => r.PricingPlanId == planId && r.PricingPlanRuleId == ruleId, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        db.PricingPlanRules.Remove(x);
        await db.SaveChangesAsync(ct);
    }
}
