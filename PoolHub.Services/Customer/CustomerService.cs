using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using CustomerEntity = PoolHub.Core.Entities.Customer;

namespace PoolHub.Services.Customer;

/// <summary>
/// Service quản lý thông tin khách hàng.
/// </summary>
public class CustomerService(PoolHubDbContext db, IAuditService auditService) : ICustomerService
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

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct)
    {
        var phone = request.PhoneNumber.Trim();
        var email = NormalizeEmail(request.Email);
        if (await db.Customers.AnyAsync(x => x.PhoneNumber == phone, ct))
            throw new ConflictException("Customer phone number already exists.");
        if (email is not null && await db.Customers.AnyAsync(x => x.Email == email, ct))
            throw new ConflictException("Customer email already exists.");

        var customer = new CustomerEntity
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = phone,
            Email = email,
            Note = request.Note?.Trim(),
            Status = true
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.CustomerCreated, nameof(CustomerEntity),
            customer.CustomerId, customer.PublicId,
            newValues: new { customer.FullName, customer.PhoneNumber, customer.Email },
            description: "Customer created.", ct: ct);
        return await GetCustomerAsync(customer.CustomerId, ct);
    }

    /// <inheritdoc/>
    public async Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, long actorUserId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct)
            ?? throw new NotFoundException($"Customer with ID {id} not found.");

        var email = NormalizeEmail(request.Email);
        if (email is not null && await db.Customers.AnyAsync(x => x.CustomerId != id && x.Email == email, ct))
            throw new ConflictException("Customer email already exists.");
        var oldValues = new { customer.FullName, customer.Email, customer.Note, customer.Status };
        customer.FullName = request.FullName.Trim();
        customer.Email = email;
        customer.Note = request.Note?.Trim();
        customer.Status = request.Status;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.CustomerUpdated, nameof(CustomerEntity),
            customer.CustomerId, customer.PublicId, oldValues,
            new { customer.FullName, customer.Email, customer.Note, customer.Status },
            "Customer updated.", ct);

        return await GetCustomerAsync(id, ct);
    }

    public async Task UpdateStatusAsync(long id, bool status, long actorUserId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct)
            ?? throw new NotFoundException($"Customer with ID {id} not found.");
        var oldStatus = customer.Status;
        customer.Status = status;
        customer.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await auditService.LogAsync(actorUserId, AuditActions.CustomerStatusChanged, nameof(CustomerEntity),
            customer.CustomerId, customer.PublicId, new { Status = oldStatus }, new { Status = status },
            status ? "Customer activated." : "Customer soft-deleted.", ct);
    }

    public async Task<PagedResult<CustomerBookingHistoryDto>> GetBookingHistoryAsync(long id, PaginationRequest request, CancellationToken ct)
    {
        await EnsureCustomerExistsAsync(id, ct);
        NormalizePagination(request);
        var query = db.Bookings.AsNoTracking().Where(x => x.CustomerId == id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.StartTimeUtc)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new CustomerBookingHistoryDto
            {
                BookingId = x.BookingId,
                BookingCode = x.BookingCode,
                TableId = x.TableId,
                TableName = x.TableId.HasValue
                    ? db.VenueTables.Where(t => t.TableId == x.TableId.Value).Select(t => t.TableName).FirstOrDefault()
                    : null,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                Status = x.Status
            }).ToListAsync(ct);
        return Page(items, request, total);
    }

    public async Task<PagedResult<CustomerSessionHistoryDto>> GetSessionHistoryAsync(long id, PaginationRequest request, CancellationToken ct)
    {
        await EnsureCustomerExistsAsync(id, ct);
        NormalizePagination(request);
        var query = db.Sessions.AsNoTracking().Where(x => x.CustomerId == id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.StartedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new CustomerSessionHistoryDto
            {
                SessionId = x.SessionId,
                SessionCode = x.SessionCode,
                StartedAtUtc = x.StartedAtUtc,
                EndedAtUtc = x.EndedAtUtc,
                Status = x.Status
            }).ToListAsync(ct);
        return Page(items, request, total);
    }

    public async Task<PagedResult<CustomerInvoiceHistoryDto>> GetInvoiceHistoryAsync(long id, PaginationRequest request, CancellationToken ct)
    {
        await EnsureCustomerExistsAsync(id, ct);
        NormalizePagination(request);
        var query = db.Invoices.AsNoTracking().Where(x => x.CustomerId == id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.IssuedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new CustomerInvoiceHistoryDto
            {
                InvoiceId = x.InvoiceId,
                InvoiceCode = x.InvoiceCode,
                GrandTotalAmount = x.GrandTotalAmount,
                PaidAmount = x.PaidAmount,
                PaymentStatus = x.PaymentStatus,
                Status = x.Status,
                IssuedAtUtc = x.IssuedAtUtc
            }).ToListAsync(ct);
        return Page(items, request, total);
    }

    private Task<bool> CustomerExistsAsync(long id, CancellationToken ct) =>
        db.Customers.AsNoTracking().AnyAsync(x => x.CustomerId == id, ct);

    private async Task EnsureCustomerExistsAsync(long id, CancellationToken ct)
    {
        if (!await CustomerExistsAsync(id, ct)) throw new NotFoundException("Customer not found.");
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static void NormalizePagination(PaginationRequest request)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);
    }

    private static PagedResult<T> Page<T>(List<T> items, PaginationRequest request, int total) => new()
    {
        Items = items,
        PageNumber = request.PageNumber,
        PageSize = request.PageSize,
        TotalItems = total
    };
}
