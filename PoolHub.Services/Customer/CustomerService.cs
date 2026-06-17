using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Customer;

/// <summary>
/// Service quản lý thông tin khách hàng.
/// </summary>
public class CustomerService(PoolHubDbContext db) : ICustomerService
{
    /// <inheritdoc/>
    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerQueryRequest request, CancellationToken ct)
    {
        var query = db.Customers.AsQueryable();

        // Tìm kiếm theo tên hoặc số điện thoại
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(search) ||
                c.PhoneNumber.Contains(search));
        }

        // Lọc theo trạng thái
        if (request.Status.HasValue)
            query = query.Where(c => c.Status == request.Status.Value);

        var total = await query.CountAsync(ct);

        // Đếm booking của từng customer bằng subquery
        var items = await query
            .OrderBy(c => c.FullName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Note = c.Note,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc,
                TotalBookings = db.Bookings.Count(b => b.CustomerId == c.CustomerId)
            })
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc/>
    public async Task<CustomerDto> GetCustomerAsync(long id, CancellationToken ct)
    {
        var customer = await db.Customers
            .Where(c => c.CustomerId == id)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Note = c.Note,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc,
                TotalBookings = db.Bookings.Count(b => b.CustomerId == c.CustomerId)
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Customer with ID {id} not found.");

        return customer;
    }

    /// <inheritdoc/>
    public async Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct)
            ?? throw new NotFoundException($"Customer with ID {id} not found.");

        customer.FullName = request.FullName;
        customer.Email = request.Email;
        customer.Note = request.Note;
        customer.Status = request.Status;

        await db.SaveChangesAsync(ct);

        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Note = customer.Note,
            Status = customer.Status,
            CreatedAtUtc = customer.CreatedAtUtc,
            TotalBookings = await db.Bookings.CountAsync(b => b.CustomerId == customer.CustomerId, ct)
        };
    }
}
