using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Shared;

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

    public async Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.PricingPlanRules.AsQueryable();
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
}
