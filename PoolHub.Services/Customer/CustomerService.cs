using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Customer;

public class CustomerService(PoolHubDbContext db) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerQueryRequest request, CancellationToken ct)
    {
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(keyword) ||
                x.PhoneNumber.Contains(keyword) ||
                (x.Email != null && x.Email.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = NormalizePhone(request.Phone);
            query = query.Where(x => x.PhoneNumber.Contains(phone));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CustomerId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalItems = total
        };
    }

    public async Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerId == id, ct)
            ?? throw new NotFoundException("Customer not found.");

        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, long? actorUserId, CancellationToken ct)
    {
        ValidateRequired(request.FullName, request.PhoneNumber);
        var phone = NormalizePhone(request.PhoneNumber);
        var email = NormalizeEmail(request.Email);

        if (await db.Customers.AnyAsync(x => x.PhoneNumber == phone, ct))
        {
            throw new ConflictException("Customer phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(email) && await db.Customers.AnyAsync(x => x.Email == email, ct))
        {
            throw new ConflictException("Customer email already exists.");
        }

        var customer = new PoolHub.Core.Entities.Customer
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = phone,
            Email = email,
            Note = request.Note,
            Status = true
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        AddAudit(actorUserId, "CUSTOMER_CREATED", customer);
        await db.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(long id, UpdateCustomerRequest request, long? actorUserId, CancellationToken ct)
    {
        ValidateRequired(request.FullName, request.PhoneNumber);
        var customer = await db.Customers.FindAsync([id], ct) ?? throw new NotFoundException("Customer not found.");

        var phone = NormalizePhone(request.PhoneNumber);
        var email = NormalizeEmail(request.Email);

        if (await db.Customers.AnyAsync(x => x.CustomerId != id && x.PhoneNumber == phone, ct))
        {
            throw new ConflictException("Customer phone number already exists.");
        }

        if (!string.IsNullOrWhiteSpace(email) && await db.Customers.AnyAsync(x => x.CustomerId != id && x.Email == email, ct))
        {
            throw new ConflictException("Customer email already exists.");
        }

        customer.FullName = request.FullName.Trim();
        customer.PhoneNumber = phone;
        customer.Email = email;
        customer.Note = request.Note;
        customer.Status = request.Status;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(actorUserId, "CUSTOMER_UPDATED", customer);
        await db.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    public async Task UpdateStatusAsync(long id, bool status, long? actorUserId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([id], ct) ?? throw new NotFoundException("Customer not found.");
        customer.Status = status;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(actorUserId, status ? "CUSTOMER_ACTIVATED" : "CUSTOMER_DEACTIVATED", customer);
        await db.SaveChangesAsync(ct);
    }

    private static CustomerDto ToDto(PoolHub.Core.Entities.Customer customer) => new()
    {
        CustomerId = customer.CustomerId,
        PublicId = customer.PublicId,
        FullName = customer.FullName,
        PhoneNumber = customer.PhoneNumber,
        Email = customer.Email,
        Note = customer.Note,
        Status = customer.Status
    };

    private static void ValidateRequired(string fullName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationException("Customer full name is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ValidationException("Customer phone number is required.");
        }
    }

    private static string NormalizePhone(string? phoneNumber) => (phoneNumber ?? string.Empty).Trim();
    private static string? NormalizeEmail(string? email) => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private void AddAudit(long? actorUserId, string action, PoolHub.Core.Entities.Customer customer)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityName = "Customer",
            EntityId = customer.CustomerId,
            EntityPublicId = customer.PublicId,
            Description = $"{action} {customer.FullName}",
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
