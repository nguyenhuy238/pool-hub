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
        var name = d.Name.Trim();
        if (await db.PricingPlans.AnyAsync(x => x.Name == name, ct))
            throw new ConflictException("Pricing plan name already exists.");
        if (d.IsDefault) await ClearOtherDefaultsAsync(null, ct);
        var x = new PoolHub.Core.Entities.PricingPlan { Name = name, IsDefault = d.IsDefault, IsActive = d.IsActive };
        db.PricingPlans.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(x.PricingPlanId, ct);
    }

    public async Task<PricingPlanDto> UpdatePricingPlanAsync(long id, PricingPlanDto d, CancellationToken ct)
    {
        var x = await db.PricingPlans.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlan not found.");
        var name = d.Name.Trim();
        if (await db.PricingPlans.AnyAsync(p => p.PricingPlanId != id && p.Name == name, ct))
            throw new ConflictException("Pricing plan name already exists.");
        if (d.IsDefault) await ClearOtherDefaultsAsync(id, ct);
        x.Name = name;
        x.IsDefault = d.IsDefault;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(id, ct);
    }

    public async Task DeletePricingPlanAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingPlans.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlan not found.");
        if (x.IsDefault) throw new BusinessRuleException("The default pricing plan cannot be deleted.");
        if (await db.PricingPlanRules.AnyAsync(r => r.PricingPlanId == id && r.IsActive, ct))
            throw new ConflictException("Pricing plan still has active rules.");
        x.IsActive = false;
        x.UpdatedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.PricingPlanRules.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime, MinimumMinutes = x.MinimumMinutes, BillingBlockMinutes = x.BillingBlockMinutes, HourlyRate = x.HourlyRate }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<PricingPlanRuleDto> GetPricingPlanRuleAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FindAsync([id], ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        return new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, StartTime = x.StartTime, EndTime = x.EndTime, MinimumMinutes = x.MinimumMinutes, BillingBlockMinutes = x.BillingBlockMinutes, HourlyRate = x.HourlyRate };
    }

    public async Task<PricingPlanRuleDto> CreatePricingPlanRuleAsync(long planId, PricingPlanRuleDto d, CancellationToken ct)
    {
        await ValidatePricingRuleAsync(planId, null, d, ct);
        var x = new PoolHub.Core.Entities.PricingPlanRule { PricingPlanId = planId, TableTypeId = d.TableTypeId, DayOfWeek = d.DayOfWeek, StartTime = d.StartTime, EndTime = d.EndTime, MinimumMinutes = d.MinimumMinutes, BillingBlockMinutes = d.BillingBlockMinutes, HourlyRate = d.HourlyRate };
        db.PricingPlanRules.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(x.PricingPlanRuleId, ct);
    }

    public async Task<PricingPlanRuleDto> UpdatePricingPlanRuleAsync(long planId, long ruleId, PricingPlanRuleDto d, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FirstOrDefaultAsync(r => r.PricingPlanId == planId && r.PricingPlanRuleId == ruleId, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        await ValidatePricingRuleAsync(planId, ruleId, d, ct);
        x.TableTypeId = d.TableTypeId;
        x.DayOfWeek = d.DayOfWeek;
        x.StartTime = d.StartTime;
        x.EndTime = d.EndTime;
        x.MinimumMinutes = d.MinimumMinutes;
        x.BillingBlockMinutes = d.BillingBlockMinutes;
        x.HourlyRate = d.HourlyRate;
        await db.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(ruleId, ct);
    }

    public async Task DeletePricingPlanRuleAsync(long planId, long ruleId, CancellationToken ct)
    {
        var x = await db.PricingPlanRules.FirstOrDefaultAsync(r => r.PricingPlanId == planId && r.PricingPlanRuleId == ruleId, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        x.IsActive = false;
        x.UpdatedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task ClearOtherDefaultsAsync(long? exceptId, CancellationToken ct)
    {
        var defaults = await db.PricingPlans
            .Where(x => x.IsDefault && (!exceptId.HasValue || x.PricingPlanId != exceptId.Value))
            .ToListAsync(ct);
        foreach (var plan in defaults)
        {
            plan.IsDefault = false;
            plan.UpdatedAtUtc = _clock.UtcNow;
        }
    }

    private async Task ValidatePricingRuleAsync(long planId, long? ruleId, PricingPlanRuleDto rule, CancellationToken ct)
    {
        if (!await db.PricingPlans.AnyAsync(x => x.PricingPlanId == planId && x.IsActive, ct))
            throw new ValidationException("Pricing plan is invalid or inactive.");
        if (!await db.TableTypes.AnyAsync(x => x.TableTypeId == rule.TableTypeId && x.IsActive, ct))
            throw new ValidationException("Table type is invalid or inactive.");
        if (rule.StartTime < TimeSpan.Zero || rule.EndTime > TimeSpan.FromDays(1) || rule.EndTime <= rule.StartTime)
            throw new ValidationException("Pricing rule end time must be after start time within one day.");
        if (rule.MinimumMinutes <= 0 || rule.BillingBlockMinutes <= 0 || rule.HourlyRate <= 0)
            throw new ValidationException("Pricing rule values must be greater than zero.");

        var overlaps = await db.PricingPlanRules.AnyAsync(x =>
            x.PricingPlanId == planId &&
            x.PricingPlanRuleId != ruleId &&
            x.TableTypeId == rule.TableTypeId &&
            x.DayOfWeek == rule.DayOfWeek &&
            x.IsActive &&
            x.StartTime < rule.EndTime &&
            x.EndTime > rule.StartTime, ct);
        if (overlaps) throw new ConflictException("Pricing rule overlaps an existing active time rule.");
    }
}
