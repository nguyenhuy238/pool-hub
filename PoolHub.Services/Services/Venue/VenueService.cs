using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Repositories;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services.Venue;

public class VenueService(IVenueRepository repo) : IVenueService
{
    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };

    // Floor
    public async Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetFloors().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<FloorDto> GetFloorAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetFloorByIdAsync(id, ct) ?? throw new NotFoundException("Floor not found.");
        return new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive };
    }
    public async Task<FloorDto> CreateFloorAsync(FloorDto d, CancellationToken ct)
    {
        var x = new Floor { Name = d.Name, IsActive = d.IsActive, DisplayOrder = d.FloorId };
        await repo.AddFloorAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetFloorAsync(x.FloorId, ct);
    }
    public async Task<FloorDto> UpdateFloorAsync(int id, FloorDto d, CancellationToken ct)
    {
        var x = await repo.GetFloorByIdAsync(id, ct) ?? throw new NotFoundException("Floor not found.");
        x.Name = d.Name; x.IsActive = d.IsActive;
        await repo.SaveChangesAsync(ct);
        return await GetFloorAsync(id, ct);
    }
    public async Task DeleteFloorAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetFloorByIdAsync(id, ct) ?? throw new NotFoundException("Floor not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<FloorDto>> GetAllFloorsAsync(CancellationToken ct) => await repo.GetFloors().Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);

    // Zone
    public async Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetZones().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<ZoneDto> GetZoneAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetZoneByIdAsync(id, ct) ?? throw new NotFoundException("Zone not found.");
        return new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive };
    }
    public async Task<ZoneDto> CreateZoneAsync(ZoneDto d, CancellationToken ct)
    {
        var x = new Zone { FloorId = d.FloorId, Name = d.Name, IsActive = d.IsActive, DisplayOrder = 1 };
        await repo.AddZoneAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetZoneAsync(x.ZoneId, ct);
    }
    public async Task<ZoneDto> UpdateZoneAsync(int id, ZoneDto d, CancellationToken ct)
    {
        var x = await repo.GetZoneByIdAsync(id, ct) ?? throw new NotFoundException("Zone not found.");
        x.Name = d.Name; x.FloorId = d.FloorId; x.IsActive = d.IsActive;
        await repo.SaveChangesAsync(ct);
        return await GetZoneAsync(id, ct);
    }
    public async Task DeleteZoneAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetZoneByIdAsync(id, ct) ?? throw new NotFoundException("Zone not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<ZoneDto>> GetAllZonesAsync(CancellationToken ct) => await repo.GetZones().Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive }).ToListAsync(ct);

    // TableType
    public async Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetTableTypes().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<TableTypeDto> GetTableTypeAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetTableTypeByIdAsync(id, ct) ?? throw new NotFoundException("TableType not found.");
        return new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity };
    }
    public async Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto d, CancellationToken ct)
    {
        var x = new TableType { Name = d.Name, Code = d.Code, DefaultCapacity = d.DefaultCapacity, IsActive = true };
        await repo.AddTableTypeAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetTableTypeAsync(x.TableTypeId, ct);
    }
    public async Task<TableTypeDto> UpdateTableTypeAsync(int id, TableTypeDto d, CancellationToken ct)
    {
        var x = await repo.GetTableTypeByIdAsync(id, ct) ?? throw new NotFoundException("TableType not found.");
        x.Name = d.Name; x.Code = d.Code; x.DefaultCapacity = d.DefaultCapacity;
        await repo.SaveChangesAsync(ct);
        return await GetTableTypeAsync(id, ct);
    }
    public async Task DeleteTableTypeAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetTableTypeByIdAsync(id, ct) ?? throw new NotFoundException("TableType not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<TableTypeDto>> GetAllTableTypesAsync(CancellationToken ct) => await repo.GetTableTypes().Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity }).ToListAsync(ct);

    // VenueTable
    public async Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetVenueTables().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<VenueTableDto> GetVenueTableAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetVenueTableByIdAsync(id, ct) ?? throw new NotFoundException("VenueTable not found.");
        return new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus, IsActive = x.IsActive };
    }
    public async Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto d, CancellationToken ct)
    {
        var x = new VenueTable { ZoneId = d.ZoneId, TableTypeId = d.TableTypeId, TableCode = d.TableCode, TableName = d.TableName, Capacity = d.Capacity, OperationalStatus = d.OperationalStatus, IsActive = d.IsActive };
        await repo.AddVenueTableAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetVenueTableAsync(x.TableId, ct);
    }
    public async Task<VenueTableDto> UpdateVenueTableAsync(int id, VenueTableDto d, CancellationToken ct)
    {
        var x = await repo.GetVenueTableByIdAsync(id, ct) ?? throw new NotFoundException("VenueTable not found.");
        x.ZoneId = d.ZoneId; x.TableTypeId = d.TableTypeId; x.TableCode = d.TableCode; x.TableName = d.TableName; x.Capacity = d.Capacity; x.OperationalStatus = d.OperationalStatus; x.IsActive = d.IsActive;
        await repo.SaveChangesAsync(ct);
        return await GetVenueTableAsync(id, ct);
    }
    public async Task DeleteVenueTableAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetVenueTableByIdAsync(id, ct) ?? throw new NotFoundException("VenueTable not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<VenueTableDto>> GetAllTablesAsync(CancellationToken ct) => await repo.GetVenueTables().Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus }).ToListAsync(ct);

    // PricingPlan
    public async Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetPricingPlans().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<PricingPlanDto> GetPricingPlanAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlan not found.");
        return new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive };
    }
    public async Task<PricingPlanDto> CreatePricingPlanAsync(PricingPlanDto d, CancellationToken ct)
    {
        var x = new PricingPlan { Name = d.Name, IsDefault = d.IsDefault, IsActive = d.IsActive };
        await repo.AddPricingPlanAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(x.PricingPlanId, ct);
    }
    public async Task<PricingPlanDto> UpdatePricingPlanAsync(int id, PricingPlanDto d, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlan not found.");
        x.Name = d.Name; x.IsDefault = d.IsDefault; x.IsActive = d.IsActive;
        await repo.SaveChangesAsync(ct);
        return await GetPricingPlanAsync(id, ct);
    }
    public async Task DeletePricingPlanAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlan not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<PricingPlanDto>> GetAllPricingPlansAsync(CancellationToken ct) => await repo.GetPricingPlans().Select(x => new PricingPlanDto { PricingPlanId = x.PricingPlanId, Name = x.Name, IsDefault = x.IsDefault, IsActive = x.IsActive }).ToListAsync(ct);

    // PricingPlanRule
    public async Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = repo.GetPricingPlanRules().Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }
    public async Task<PricingPlanRuleDto> GetPricingPlanRuleAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanRuleByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        return new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate };
    }
    public async Task<PricingPlanRuleDto> CreatePricingPlanRuleAsync(PricingPlanRuleDto d, CancellationToken ct)
    {
        var x = new PricingPlanRule { PricingPlanId = d.PricingPlanId, TableTypeId = d.TableTypeId, DayOfWeek = d.DayOfWeek, HourlyRate = d.HourlyRate, IsActive = true };
        await repo.AddPricingPlanRuleAsync(x, ct);
        await repo.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(x.PricingPlanRuleId, ct);
    }
    public async Task<PricingPlanRuleDto> UpdatePricingPlanRuleAsync(int id, PricingPlanRuleDto d, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanRuleByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        x.PricingPlanId = d.PricingPlanId; x.TableTypeId = d.TableTypeId; x.DayOfWeek = d.DayOfWeek; x.HourlyRate = d.HourlyRate;
        await repo.SaveChangesAsync(ct);
        return await GetPricingPlanRuleAsync(id, ct);
    }
    public async Task DeletePricingPlanRuleAsync(int id, CancellationToken ct)
    {
        var x = await repo.GetPricingPlanRuleByIdAsync(id, ct) ?? throw new NotFoundException("PricingPlanRule not found.");
        x.IsActive = false;
        await repo.SaveChangesAsync(ct);
    }
    public async Task<IEnumerable<PricingPlanRuleDto>> GetAllPricingPlanRulesAsync(CancellationToken ct) => await repo.GetPricingPlanRules().Select(x => new PricingPlanRuleDto { PricingPlanRuleId = x.PricingPlanRuleId, PricingPlanId = x.PricingPlanId, TableTypeId = x.TableTypeId, DayOfWeek = x.DayOfWeek, HourlyRate = x.HourlyRate }).ToListAsync(ct);
}
