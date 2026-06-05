using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Entities;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Common;

public partial class CrudService
{
    public async Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.Floors.Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<FloorDto> GetFloorAsync(long id, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        return new FloorDto { FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive };
    }

    public async Task<FloorDto> CreateFloorAsync(FloorDto d, CancellationToken ct)
    {
        var x = new Floor { Name = d.Name, Description = d.Description, DisplayOrder = d.DisplayOrder, IsActive = true };
        db.Floors.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetFloorAsync(x.FloorId, ct);
    }

    public async Task<FloorDto> UpdateFloorAsync(long id, FloorDto d, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        x.Name = d.Name;
        x.Description = d.Description;
        x.DisplayOrder = d.DisplayOrder;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetFloorAsync(id, ct);
    }

    public async Task DeleteFloorAsync(long id, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.Zones.Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<ZoneDto> GetZoneAsync(long id, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        return new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive };
    }

    public async Task<ZoneDto> CreateZoneAsync(ZoneDto d, CancellationToken ct)
    {
        var x = new Zone { FloorId = d.FloorId, Name = d.Name, Description = d.Description, DisplayOrder = d.DisplayOrder, IsActive = true };
        db.Zones.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetZoneAsync(x.ZoneId, ct);
    }

    public async Task<ZoneDto> UpdateZoneAsync(long id, ZoneDto d, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        x.Name = d.Name;
        x.FloorId = d.FloorId;
        x.Description = d.Description;
        x.DisplayOrder = d.DisplayOrder;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetZoneAsync(id, ct);
    }

    public async Task DeleteZoneAsync(long id, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.TableTypes.Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity, Description = x.Description, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<TableTypeDto> GetTableTypeAsync(long id, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        return new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity, Description = x.Description, IsActive = x.IsActive };
    }

    public async Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto d, CancellationToken ct)
    {
        var x = new TableType { Name = d.Name, Code = d.Code, DefaultCapacity = d.DefaultCapacity, Description = d.Description, IsActive = true };
        db.TableTypes.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetTableTypeAsync(x.TableTypeId, ct);
    }

    public async Task<TableTypeDto> UpdateTableTypeAsync(long id, TableTypeDto d, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        x.Name = d.Name;
        x.Code = d.Code;
        x.DefaultCapacity = d.DefaultCapacity;
        x.Description = d.Description;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetTableTypeAsync(id, ct);
    }

    public async Task DeleteTableTypeAsync(long id, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.VenueTables.Where(x => x.IsActive);
        var t = await q.CountAsync(ct);
        var i = await q.Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<VenueTableDto> GetVenueTableAsync(long id, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        return new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus, IsActive = x.IsActive };
    }

    public async Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto d, CancellationToken ct)
    {
        var x = new VenueTable { ZoneId = d.ZoneId, TableTypeId = d.TableTypeId, TableCode = d.TableCode, TableName = d.TableName, Capacity = d.Capacity, OperationalStatus = d.OperationalStatus, IsActive = true };
        db.VenueTables.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetVenueTableAsync(x.TableId, ct);
    }

    public async Task<VenueTableDto> UpdateVenueTableAsync(long id, VenueTableDto d, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        x.ZoneId = d.ZoneId;
        x.TableTypeId = d.TableTypeId;
        x.TableCode = d.TableCode;
        x.TableName = d.TableName;
        x.Capacity = d.Capacity;
        x.OperationalStatus = d.OperationalStatus;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetVenueTableAsync(id, ct);
    }

    public async Task DeleteVenueTableAsync(long id, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }
}
