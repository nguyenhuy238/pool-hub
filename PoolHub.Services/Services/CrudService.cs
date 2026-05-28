using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Product;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Services;

public class CrudService(PoolHubDbContext db) : ICrudService
{
    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };

    public async Task<PagedResult<FloorDto>> GetFloorsAsync(PaginationRequest r, CancellationToken ct){ var q=db.Floors.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new FloorDto{FloorId=x.FloorId,Name=x.Name,IsActive=x.IsActive}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<FloorDto> GetFloorAsync(int id, CancellationToken ct){ var x=await db.Floors.FindAsync([id],ct)??throw new NotFoundException("Floor not found."); return new FloorDto{FloorId=x.FloorId,Name=x.Name,IsActive=x.IsActive}; }
    public async Task<FloorDto> CreateFloorAsync(FloorDto d, CancellationToken ct){ var x=new Floor{Name=d.Name,IsActive=d.IsActive,DisplayOrder=d.FloorId}; db.Floors.Add(x); await db.SaveChangesAsync(ct); return await GetFloorAsync(x.FloorId,ct);}    
    public async Task<FloorDto> UpdateFloorAsync(int id, FloorDto d, CancellationToken ct){ var x=await db.Floors.FindAsync([id],ct)??throw new NotFoundException("Floor not found."); x.Name=d.Name; x.IsActive=d.IsActive; await db.SaveChangesAsync(ct); return await GetFloorAsync(id,ct);}    
    public async Task DeleteFloorAsync(int id, CancellationToken ct){ var x=await db.Floors.FindAsync([id],ct)??throw new NotFoundException("Floor not found."); db.Floors.Remove(x); await db.SaveChangesAsync(ct);}    

    public async Task<PagedResult<ZoneDto>> GetZonesAsync(PaginationRequest r, CancellationToken ct){ var q=db.Zones.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new ZoneDto{ZoneId=x.ZoneId,FloorId=x.FloorId,Name=x.Name,IsActive=x.IsActive}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<ZoneDto> GetZoneAsync(int id, CancellationToken ct){ var x=await db.Zones.FindAsync([id],ct)??throw new NotFoundException("Zone not found."); return new ZoneDto{ZoneId=x.ZoneId,FloorId=x.FloorId,Name=x.Name,IsActive=x.IsActive}; }
    public async Task<ZoneDto> CreateZoneAsync(ZoneDto d, CancellationToken ct){ var x=new Zone{FloorId=d.FloorId,Name=d.Name,IsActive=d.IsActive,DisplayOrder=1}; db.Zones.Add(x); await db.SaveChangesAsync(ct); return await GetZoneAsync(x.ZoneId,ct);}    
    public async Task<ZoneDto> UpdateZoneAsync(int id, ZoneDto d, CancellationToken ct){ var x=await db.Zones.FindAsync([id],ct)??throw new NotFoundException("Zone not found."); x.Name=d.Name; x.FloorId=d.FloorId; x.IsActive=d.IsActive; await db.SaveChangesAsync(ct); return await GetZoneAsync(id,ct);}    
    public async Task DeleteZoneAsync(int id, CancellationToken ct){ var x=await db.Zones.FindAsync([id],ct)??throw new NotFoundException("Zone not found."); db.Zones.Remove(x); await db.SaveChangesAsync(ct);}    

    public async Task<PagedResult<TableTypeDto>> GetTableTypesAsync(PaginationRequest r, CancellationToken ct){ var q=db.TableTypes.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new TableTypeDto{TableTypeId=x.TableTypeId,Name=x.Name,Code=x.Code,DefaultCapacity=x.DefaultCapacity}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<TableTypeDto> GetTableTypeAsync(int id, CancellationToken ct){ var x=await db.TableTypes.FindAsync([id],ct)??throw new NotFoundException("TableType not found."); return new TableTypeDto{TableTypeId=x.TableTypeId,Name=x.Name,Code=x.Code,DefaultCapacity=x.DefaultCapacity}; }
    public async Task<TableTypeDto> CreateTableTypeAsync(TableTypeDto d, CancellationToken ct){ var x=new TableType{Name=d.Name,Code=d.Code,DefaultCapacity=d.DefaultCapacity,IsActive=true}; db.TableTypes.Add(x); await db.SaveChangesAsync(ct); return await GetTableTypeAsync(x.TableTypeId,ct);}    
    public async Task<TableTypeDto> UpdateTableTypeAsync(int id, TableTypeDto d, CancellationToken ct){ var x=await db.TableTypes.FindAsync([id],ct)??throw new NotFoundException("TableType not found."); x.Name=d.Name; x.Code=d.Code; x.DefaultCapacity=d.DefaultCapacity; await db.SaveChangesAsync(ct); return await GetTableTypeAsync(id,ct);}    
    public async Task DeleteTableTypeAsync(int id, CancellationToken ct){ var x=await db.TableTypes.FindAsync([id],ct)??throw new NotFoundException("TableType not found."); db.TableTypes.Remove(x); await db.SaveChangesAsync(ct);}    

    public async Task<PagedResult<VenueTableDto>> GetVenueTablesAsync(PaginationRequest r, CancellationToken ct){ var q=db.VenueTables.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new VenueTableDto{TableId=x.TableId,ZoneId=x.ZoneId,TableTypeId=x.TableTypeId,TableCode=x.TableCode,TableName=x.TableName,Capacity=x.Capacity,OperationalStatus=x.OperationalStatus}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<VenueTableDto> GetVenueTableAsync(int id, CancellationToken ct){ var x=await db.VenueTables.FindAsync([id],ct)??throw new NotFoundException("VenueTable not found."); return new VenueTableDto{TableId=x.TableId,ZoneId=x.ZoneId,TableTypeId=x.TableTypeId,TableCode=x.TableCode,TableName=x.TableName,Capacity=x.Capacity,OperationalStatus=x.OperationalStatus}; }
    public async Task<VenueTableDto> CreateVenueTableAsync(VenueTableDto d, CancellationToken ct){ var x=new VenueTable{ZoneId=d.ZoneId,TableTypeId=d.TableTypeId,TableCode=d.TableCode,TableName=d.TableName,Capacity=d.Capacity,OperationalStatus=d.OperationalStatus}; db.VenueTables.Add(x); await db.SaveChangesAsync(ct); return await GetVenueTableAsync(x.TableId,ct);}    
    public async Task<VenueTableDto> UpdateVenueTableAsync(int id, VenueTableDto d, CancellationToken ct){ var x=await db.VenueTables.FindAsync([id],ct)??throw new NotFoundException("VenueTable not found."); x.ZoneId=d.ZoneId; x.TableTypeId=d.TableTypeId; x.TableCode=d.TableCode; x.TableName=d.TableName; x.Capacity=d.Capacity; x.OperationalStatus=d.OperationalStatus; await db.SaveChangesAsync(ct); return await GetVenueTableAsync(id,ct);}    
    public async Task DeleteVenueTableAsync(int id, CancellationToken ct){ var x=await db.VenueTables.FindAsync([id],ct)??throw new NotFoundException("VenueTable not found."); db.VenueTables.Remove(x); await db.SaveChangesAsync(ct);}    

    public async Task<PagedResult<PricingPlanDto>> GetPricingPlansAsync(PaginationRequest r, CancellationToken ct){ var q=db.PricingPlans.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new PricingPlanDto{PricingPlanId=x.PricingPlanId,Name=x.Name,IsDefault=x.IsDefault,IsActive=x.IsActive}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<PagedResult<PricingPlanRuleDto>> GetPricingPlanRulesAsync(PaginationRequest r, CancellationToken ct){ var q=db.PricingPlanRules.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new PricingPlanRuleDto{PricingPlanRuleId=x.PricingPlanRuleId,PricingPlanId=x.PricingPlanId,TableTypeId=x.TableTypeId,DayOfWeek=x.DayOfWeek,HourlyRate=x.HourlyRate}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    

    public async Task<PagedResult<ProductCategoryDto>> GetProductCategoriesAsync(PaginationRequest r, CancellationToken ct){ var q=db.ProductCategories.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((r.PageNumber-1)*r.PageSize).Take(r.PageSize).Select(x=>new ProductCategoryDto{CategoryId=x.CategoryId,Name=x.Name,Code=x.Code}).ToListAsync(ct); return Page(i,r.PageNumber,r.PageSize,t);}    
    public async Task<ProductCategoryDto> CreateProductCategoryAsync(ProductCategoryDto d, CancellationToken ct){ var x=new ProductCategory{Name=d.Name,Code=d.Code,IsActive=true}; db.ProductCategories.Add(x); await db.SaveChangesAsync(ct); return new ProductCategoryDto{CategoryId=x.CategoryId,Name=x.Name,Code=x.Code}; }

    public async Task<PagedResult<BookingDto>> GetBookingsCrudAsync(PaginationRequest request, CancellationToken ct){ var q=db.Bookings.AsQueryable(); var t=await q.CountAsync(ct); var i=await q.Skip((request.PageNumber-1)*request.PageSize).Take(request.PageSize).Select(x=>new BookingDto{BookingId=x.BookingId,BookingCode=x.BookingCode,CustomerId=x.CustomerId,TableId=x.TableId,StartTimeUtc=x.StartTimeUtc,EndTimeUtc=x.EndTimeUtc,Status=x.Status}).ToListAsync(ct); return Page(i,request.PageNumber,request.PageSize,t);}    
    public async Task<BookingDto> GetBookingAsync(int id, CancellationToken ct){ var x=await db.Bookings.FindAsync([id],ct)??throw new NotFoundException("Booking not found."); return new BookingDto{BookingId=x.BookingId,BookingCode=x.BookingCode,CustomerId=x.CustomerId,TableId=x.TableId,StartTimeUtc=x.StartTimeUtc,EndTimeUtc=x.EndTimeUtc,Status=x.Status}; }
    public async Task DeleteBookingAsync(int id, CancellationToken ct){ var x=await db.Bookings.FindAsync([id],ct)??throw new NotFoundException("Booking not found."); db.Bookings.Remove(x); await db.SaveChangesAsync(ct);}    
}
