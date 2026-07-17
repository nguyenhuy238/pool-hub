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
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var search = r.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }
        var t = await q.CountAsync(ct);
        var i = await q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.FloorId).Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new FloorDto { FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<FloorDto> GetFloorAsync(long id, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        return new FloorDto { FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive };
    }

    public async Task<FloorDto> CreateFloorAsync(FloorDto d, CancellationToken ct)
    {
        var name = d.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Floor name is required.");
        if (d.DisplayOrder < 0) throw new ValidationException("Display order must be non-negative.");
        if (await db.Floors.AnyAsync(x => x.IsActive && x.Name == name, ct)) throw new ConflictException("Floor name already exists.");
        var x = new Floor { Name = name, Description = d.Description?.Trim(), DisplayOrder = d.DisplayOrder, IsActive = true };
        db.Floors.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetFloorAsync(x.FloorId, ct);
    }

    public async Task<FloorDto> UpdateFloorAsync(long id, FloorDto d, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        var name = d.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Floor name is required.");
        if (d.DisplayOrder < 0) throw new ValidationException("Display order must be non-negative.");
        if (await db.Floors.AnyAsync(f => f.FloorId != id && f.IsActive && f.Name == name, ct)) throw new ConflictException("Floor name already exists.");
        x.Name = name;
        x.Description = d.Description?.Trim();
        x.DisplayOrder = d.DisplayOrder;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetFloorAsync(id, ct);
    }

    public async Task DeleteFloorAsync(long id, CancellationToken ct)
    {
        var x = await db.Floors.FindAsync([id], ct) ?? throw new NotFoundException("Floor not found.");
        if (await db.Zones.AnyAsync(z => z.FloorId == id && z.IsActive, ct))
            throw new ConflictException("Cannot delete a floor that still has active zones.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.Zones.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var search = r.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }
        var t = await q.CountAsync(ct);
        var i = await q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.ZoneId).Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, Description = x.Description, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<ZoneDto> GetZoneAsync(long id, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        return new ZoneDto { ZoneId = x.ZoneId, FloorId = x.FloorId, Name = x.Name, IsActive = x.IsActive };
    }

    public async Task<ZoneDto> CreateZoneAsync(ZoneDto d, CancellationToken ct)
    {
        var name = d.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Zone name is required.");
        if (!await db.Floors.AnyAsync(x => x.FloorId == d.FloorId && x.IsActive, ct)) throw new ValidationException("Floor is invalid or inactive.");
        if (d.DisplayOrder < 0) throw new ValidationException("Display order must be non-negative.");
        if (await db.Zones.AnyAsync(x => x.IsActive && x.FloorId == d.FloorId && x.Name == name, ct)) throw new ConflictException("Zone name already exists on this floor.");
        var x = new Zone { FloorId = d.FloorId, Name = name, Description = d.Description?.Trim(), DisplayOrder = d.DisplayOrder, IsActive = true };
        db.Zones.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetZoneAsync(x.ZoneId, ct);
    }

    public async Task<ZoneDto> UpdateZoneAsync(long id, ZoneDto d, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        var name = d.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Zone name is required.");
        if (!await db.Floors.AnyAsync(f => f.FloorId == d.FloorId && f.IsActive, ct)) throw new ValidationException("Floor is invalid or inactive.");
        if (d.DisplayOrder < 0) throw new ValidationException("Display order must be non-negative.");
        if (await db.Zones.AnyAsync(z => z.ZoneId != id && z.IsActive && z.FloorId == d.FloorId && z.Name == name, ct)) throw new ConflictException("Zone name already exists on this floor.");
        x.Name = name;
        x.FloorId = d.FloorId;
        x.Description = d.Description?.Trim();
        x.DisplayOrder = d.DisplayOrder;
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetZoneAsync(id, ct);
    }

    public async Task DeleteZoneAsync(long id, CancellationToken ct)
    {
        var x = await db.Zones.FindAsync([id], ct) ?? throw new NotFoundException("Zone not found.");
        if (await db.VenueTables.AnyAsync(t => t.ZoneId == id && t.IsActive, ct))
            throw new ConflictException("Cannot delete a zone that still has active tables.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.TableTypes.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var search = r.Search.Trim();
            q = q.Where(x => x.Name.Contains(search) || x.Code.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }
        var t = await q.CountAsync(ct);
        var i = await q.OrderBy(x => x.TableTypeId).Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity, Description = x.Description, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<TableTypeDto> GetTableTypeAsync(long id, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        return new TableTypeDto { TableTypeId = x.TableTypeId, Name = x.Name, Code = x.Code, DefaultCapacity = x.DefaultCapacity };
    }

    public async Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto d, CancellationToken ct)
    {
        var name = d.Name.Trim();
        var code = d.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) throw new ValidationException("Table type name and code are required.");
        if (d.DefaultCapacity <= 0) throw new ValidationException("Default capacity must be greater than zero.");
        if (await db.TableTypes.AnyAsync(x => x.IsActive && x.Code == code, ct)) throw new ConflictException("Table type code already exists.");
        var x = new TableType { Name = name, Code = code, DefaultCapacity = d.DefaultCapacity, Description = d.Description?.Trim(), IsActive = true };
        db.TableTypes.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetTableTypeAsync(x.TableTypeId, ct);
    }

    public async Task<TableTypeDto> UpdateTableTypeAsync(long id, TableTypeDto d, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        var name = d.Name.Trim();
        var code = d.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code)) throw new ValidationException("Table type name and code are required.");
        if (d.DefaultCapacity <= 0) throw new ValidationException("Default capacity must be greater than zero.");
        if (await db.TableTypes.AnyAsync(t => t.TableTypeId != id && t.IsActive && t.Code == code, ct)) throw new ConflictException("Table type code already exists.");
        x.Name = name;
        x.Code = code;
        x.DefaultCapacity = d.DefaultCapacity;
        x.Description = d.Description?.Trim();
        x.IsActive = d.IsActive;
        await db.SaveChangesAsync(ct);
        return await GetTableTypeAsync(id, ct);
    }

    public async Task DeleteTableTypeAsync(long id, CancellationToken ct)
    {
        var x = await db.TableTypes.FindAsync([id], ct) ?? throw new NotFoundException("TableType not found.");
        if (await db.VenueTables.AnyAsync(t => t.TableTypeId == id && t.IsActive, ct))
            throw new ConflictException("Cannot delete a table type that is still used by active tables.");
        if (await db.PricingPlanRules.AnyAsync(r => r.TableTypeId == id && r.IsActive, ct))
            throw new ConflictException("Cannot delete a table type that is still used by active pricing rules.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest r, CancellationToken ct)
    {
        var q = db.VenueTables.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var search = r.Search.Trim();
            q = q.Where(x => x.TableCode.Contains(search) || x.TableName.Contains(search));
        }
        var t = await q.CountAsync(ct);
        var i = await q.OrderBy(x => x.TableCode).Skip((r.PageNumber - 1) * r.PageSize).Take(r.PageSize).Select(x => new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus, IsActive = x.IsActive }).ToListAsync(ct);
        return Page(i, r.PageNumber, r.PageSize, t);
    }

    public async Task<VenueTableDto> GetVenueTableAsync(long id, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        return new VenueTableDto { TableId = x.TableId, ZoneId = x.ZoneId, TableTypeId = x.TableTypeId, TableCode = x.TableCode, TableName = x.TableName, Capacity = x.Capacity, OperationalStatus = x.OperationalStatus };
    }

    public async Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto d, CancellationToken ct)
    {
        await ValidateVenueTableAsync(null, d, ct);
        var x = new VenueTable { ZoneId = d.ZoneId, TableTypeId = d.TableTypeId, TableCode = d.TableCode.Trim().ToUpperInvariant(), TableName = d.TableName.Trim(), Capacity = d.Capacity, OperationalStatus = d.OperationalStatus, IsActive = true };
        db.VenueTables.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetVenueTableAsync(x.TableId, ct);
    }

    public async Task<VenueTableDto> UpdateVenueTableAsync(long id, VenueTableDto d, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        await ValidateVenueTableAsync(id, d, ct);
        if (await HasActiveSessionAsync(id, ct) && d.OperationalStatus != 2)
            throw new BusinessRuleException("Cannot change an occupied table to another status while a session is active.");
        x.ZoneId = d.ZoneId;
        x.TableTypeId = d.TableTypeId;
        x.TableCode = d.TableCode.Trim().ToUpperInvariant();
        x.TableName = d.TableName.Trim();
        x.Capacity = d.Capacity;
        x.OperationalStatus = d.OperationalStatus;
        await db.SaveChangesAsync(ct);
        return await GetVenueTableAsync(id, ct);
    }

    public async Task DeleteVenueTableAsync(long id, CancellationToken ct)
    {
        var x = await db.VenueTables.FindAsync([id], ct) ?? throw new NotFoundException("VenueTable not found.");
        if (await HasActiveSessionAsync(id, ct))
            throw new ConflictException("Cannot delete a table with an active session.");
        if (await db.Bookings.AnyAsync(b => b.TableId == id && (b.Status == 1 || b.Status == 2) && b.EndTimeUtc > _clock.UtcNow, ct))
            throw new ConflictException("Cannot delete a table with active or upcoming bookings.");
        x.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateVenueTableAsync(long? id, VenueTableDto d, CancellationToken ct)
    {
        var code = d.TableCode.Trim().ToUpperInvariant();
        var name = d.TableName.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) throw new ValidationException("Table code and name are required.");
        if (d.Capacity <= 0) throw new ValidationException("Capacity must be greater than zero.");
        if (d.OperationalStatus is < 1 or > 5) throw new ValidationException("Operational status is invalid.");
        if (!await db.Zones.AnyAsync(x => x.ZoneId == d.ZoneId && x.IsActive, ct)) throw new ValidationException("Zone is invalid or inactive.");
        if (!await db.TableTypes.AnyAsync(x => x.TableTypeId == d.TableTypeId && x.IsActive, ct)) throw new ValidationException("Table type is invalid or inactive.");
        if (await db.VenueTables.AnyAsync(x => x.TableId != id && x.IsActive && x.TableCode == code, ct)) throw new ConflictException("Table code already exists.");
    }

    private Task<bool> HasActiveSessionAsync(long tableId, CancellationToken ct) =>
        db.SessionTableAssignments.AnyAsync(sta => sta.TableId == tableId && sta.EndedAtUtc == null && db.Sessions.Any(s => s.SessionId == sta.SessionId && s.Status == 1), ct);
}
