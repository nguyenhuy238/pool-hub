using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class VenueRepository(PoolHubDbContext db) : IVenueRepository
{
    // Floor
    public IQueryable<Floor> GetFloors() => db.Floors.AsQueryable();
    public async Task<Floor?> GetFloorByIdAsync(int id, CancellationToken ct) => await db.Floors.FindAsync([id], ct);
    public async Task AddFloorAsync(Floor floor, CancellationToken ct) => await db.Floors.AddAsync(floor, ct);

    // Zone
    public IQueryable<Zone> GetZones() => db.Zones.AsQueryable();
    public async Task<Zone?> GetZoneByIdAsync(int id, CancellationToken ct) => await db.Zones.FindAsync([id], ct);
    public async Task AddZoneAsync(Zone zone, CancellationToken ct) => await db.Zones.AddAsync(zone, ct);

    // TableType
    public IQueryable<TableType> GetTableTypes() => db.TableTypes.AsQueryable();
    public async Task<TableType?> GetTableTypeByIdAsync(int id, CancellationToken ct) => await db.TableTypes.FindAsync([id], ct);
    public async Task AddTableTypeAsync(TableType tableType, CancellationToken ct) => await db.TableTypes.AddAsync(tableType, ct);

    // VenueTable
    public IQueryable<VenueTable> GetVenueTables() => db.VenueTables.AsQueryable();
    public async Task<VenueTable?> GetVenueTableByIdAsync(int id, CancellationToken ct) => await db.VenueTables.FindAsync([id], ct);
    public async Task AddVenueTableAsync(VenueTable venueTable, CancellationToken ct) => await db.VenueTables.AddAsync(venueTable, ct);

    // PricingPlan
    public IQueryable<PricingPlan> GetPricingPlans() => db.PricingPlans.AsQueryable();
    public async Task<PricingPlan?> GetPricingPlanByIdAsync(int id, CancellationToken ct) => await db.PricingPlans.FindAsync([id], ct);
    public async Task AddPricingPlanAsync(PricingPlan plan, CancellationToken ct) => await db.PricingPlans.AddAsync(plan, ct);

    // PricingPlanRule
    public IQueryable<PricingPlanRule> GetPricingPlanRules() => db.PricingPlanRules.AsQueryable();
    public async Task<PricingPlanRule?> GetPricingPlanRuleByIdAsync(int id, CancellationToken ct) => await db.PricingPlanRules.FindAsync([id], ct);
    public async Task AddPricingPlanRuleAsync(PricingPlanRule rule, CancellationToken ct) => await db.PricingPlanRules.AddAsync(rule, ct);

    // Save
    public async Task<int> SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
