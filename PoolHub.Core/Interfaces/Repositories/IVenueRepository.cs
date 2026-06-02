using PoolHub.Core.Entities;

namespace PoolHub.Core.Interfaces.Repositories;

public interface IVenueRepository
{
    // Floor
    IQueryable<Floor> GetFloors();
    Task<Floor?> GetFloorByIdAsync(int id, CancellationToken ct);
    Task AddFloorAsync(Floor floor, CancellationToken ct);
    
    // Zone
    IQueryable<Zone> GetZones();
    Task<Zone?> GetZoneByIdAsync(int id, CancellationToken ct);
    Task AddZoneAsync(Zone zone, CancellationToken ct);
    
    // TableType
    IQueryable<TableType> GetTableTypes();
    Task<TableType?> GetTableTypeByIdAsync(int id, CancellationToken ct);
    Task AddTableTypeAsync(TableType tableType, CancellationToken ct);
    
    // VenueTable
    IQueryable<VenueTable> GetVenueTables();
    Task<VenueTable?> GetVenueTableByIdAsync(int id, CancellationToken ct);
    Task AddVenueTableAsync(VenueTable venueTable, CancellationToken ct);
    
    // PricingPlan
    IQueryable<PricingPlan> GetPricingPlans();
    Task<PricingPlan?> GetPricingPlanByIdAsync(int id, CancellationToken ct);
    Task AddPricingPlanAsync(PricingPlan plan, CancellationToken ct);
    
    // PricingPlanRule
    IQueryable<PricingPlanRule> GetPricingPlanRules();
    Task<PricingPlanRule?> GetPricingPlanRuleByIdAsync(int id, CancellationToken ct);
    Task AddPricingPlanRuleAsync(PricingPlanRule rule, CancellationToken ct);

    // Save changes
    Task<int> SaveChangesAsync(CancellationToken ct);
}
