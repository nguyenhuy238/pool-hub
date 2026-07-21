using Microsoft.EntityFrameworkCore;
using PoolHub.Shared;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Venue;
using PoolHub.Core.Entities;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Common;

public partial class CrudService
{
    public async Task<PagedResult<PricingSpecialDateDto>> GetPricingSpecialDatesAsync(PaginationRequest request, CancellationToken ct)
    {
        var q = db.PricingSpecialDates.AsQueryable();
        
        if (!string.IsNullOrEmpty(request.Search))
        {
            q = q.Where(x => x.Description.Contains(request.Search));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(x => x.Date)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PricingSpecialDateDto
            {
                PricingSpecialDateId = x.PricingSpecialDateId,
                Date = x.Date,
                DayType = x.DayType,
                Description = x.Description
            })
            .ToListAsync(ct);

        return Page(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<PricingSpecialDateDto> GetPricingSpecialDateAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingSpecialDates.FindAsync([id], ct) ?? throw new NotFoundException("PricingSpecialDate not found.");
        return new PricingSpecialDateDto
        {
            PricingSpecialDateId = x.PricingSpecialDateId,
            Date = x.Date,
            DayType = x.DayType,
            Description = x.Description
        };
    }

    public async Task<PricingSpecialDateDto> CreatePricingSpecialDateAsync(PricingSpecialDateDto dto, CancellationToken ct)
    {
        if (await db.PricingSpecialDates.AnyAsync(x => x.Date.Date == dto.Date.Date, ct))
        {
            throw new ConflictException("Đã tồn tại cấu hình cho ngày này.");
        }

        var x = new PricingSpecialDate
        {
            Date = dto.Date.Date,
            DayType = dto.DayType,
            Description = dto.Description
        };

        db.PricingSpecialDates.Add(x);
        await db.SaveChangesAsync(ct);
        return await GetPricingSpecialDateAsync(x.PricingSpecialDateId, ct);
    }

    public async Task<PricingSpecialDateDto> UpdatePricingSpecialDateAsync(long id, PricingSpecialDateDto dto, CancellationToken ct)
    {
        var x = await db.PricingSpecialDates.FindAsync([id], ct) ?? throw new NotFoundException("PricingSpecialDate not found.");

        if (await db.PricingSpecialDates.AnyAsync(s => s.PricingSpecialDateId != id && s.Date.Date == dto.Date.Date, ct))
        {
            throw new ConflictException("Đã tồn tại cấu hình cho ngày này.");
        }

        x.Date = dto.Date.Date;
        x.DayType = dto.DayType;
        x.Description = dto.Description;

        await db.SaveChangesAsync(ct);
        return await GetPricingSpecialDateAsync(id, ct);
    }

    public async Task DeletePricingSpecialDateAsync(long id, CancellationToken ct)
    {
        var x = await db.PricingSpecialDates.FindAsync([id], ct) ?? throw new NotFoundException("PricingSpecialDate not found.");
        db.PricingSpecialDates.Remove(x);
        await db.SaveChangesAsync(ct);
    }
}
