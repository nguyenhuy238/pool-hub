using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using CustomerEntity = PoolHub.Core.Entities.Customer;

namespace PoolHub.Services.Customer;

/// <summary>
/// Service quản lý thông tin khách hàng.
/// </summary>
public class CustomerService(PoolHubDbContext db, IAuditService auditService, IClock? clock = null) : ICustomerService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

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
                PublicId = c.PublicId,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Note = c.Note,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc,
                TotalBookings = db.Bookings.Count(b => b.CustomerId == c.CustomerId),
                LoyaltyPoints = c.LoyaltyPoints,
                TotalPointsEarned = c.TotalPointsEarned
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
                PublicId = c.PublicId,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Note = c.Note,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc,
                TotalBookings = db.Bookings.Count(b => b.CustomerId == c.CustomerId),
                LoyaltyPoints = c.LoyaltyPoints,
                TotalPointsEarned = c.TotalPointsEarned
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Customer with ID {id} not found.");

        return customer;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("Phone number is required.");
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
        customer.UpdatedAtUtc = _clock.UtcNow;
        db.Customers.Update(customer);

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
        customer.UpdatedAtUtc = _clock.UtcNow;
        db.Customers.Update(customer);
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

    public async Task<PoolHub.Core.DTOs.Admin.DiscountDto> ExchangeVoucherAsync(long customerId, long voucherTemplateId, long actorUserId, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([customerId], ct)
            ?? throw new NotFoundException("Khách hàng không tồn tại.");
        if (!customer.Status)
            throw new BusinessRuleException("Khách hàng đang bị vô hiệu hóa.");

        var template = await db.Discounts.FindAsync([voucherTemplateId], ct)
            ?? throw new NotFoundException("Gói voucher không tồn tại.");
        var now = _clock.UtcNow;
        if (!template.IsActive || !template.IsVoucher || !template.PointsRequired.HasValue || template.PointsRequired.Value <= 0 || (template.EndsAtUtc.HasValue && template.EndsAtUtc.Value <= now))
            throw new BusinessRuleException("Gói voucher này không hợp lệ hoặc đã hết thời hạn đổi thưởng.");
        
        if (customer.LoyaltyPoints < template.PointsRequired.Value)
            throw new BusinessRuleException($"Khách hàng không đủ điểm tích lũy. Cần {template.PointsRequired.Value:N0} điểm, hiện có {customer.LoyaltyPoints:N0} điểm.");

        customer.LoyaltyPoints -= template.PointsRequired.Value;
        customer.UpdatedAtUtc = now;
        db.Customers.Update(customer);

        var randomSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
        var personalCode = $"V-{customer.CustomerId}-{randomSuffix}";
        
        var personalVoucher = new PoolHub.Core.Entities.Discount
        {
            DiscountCode = personalCode,
            Name = $"{template.Name} (Đổi bởi {customer.FullName})",
            DiscountType = template.DiscountType,
            Value = template.Value,
            MaxAmount = template.MaxAmount,
            MinTimeSubtotal = template.MinTimeSubtotal,
            AppliesTo = template.AppliesTo,
            StartsAtUtc = now,
            EndsAtUtc = template.EndsAtUtc,
            IsActive = true,
            IsVoucher = true,
            PointsRequired = 0,
            CustomerId = customer.CustomerId,
            MaxUsage = 1,
            UsageCount = 0
        };
        db.Discounts.Add(personalVoucher);

        db.CustomerPointHistories.Add(new PoolHub.Core.Entities.CustomerPointHistory
        {
            CustomerId = customer.CustomerId,
            Points = -template.PointsRequired.Value,
            TransactionType = "REDEEM",
            Description = $"Đổi voucher '{template.Name}' (Mã: {personalCode}" + (template.EndsAtUtc.HasValue ? $" - HSD: {template.EndsAtUtc.Value:dd/MM/yyyy HH:mm}" : "") + ")",
            ReferenceId = personalVoucher.DiscountId,
            CreatedAtUtc = now
        });

        await db.SaveChangesAsync(ct);
        
        await auditService.LogAsync(actorUserId, AuditActions.CustomerVoucherExchanged, nameof(CustomerEntity),
            customer.CustomerId, customer.PublicId,
            new { PointsDeducted = template.PointsRequired.Value },
            new { PersonalVoucherCode = personalCode, RemainingPoints = customer.LoyaltyPoints },
            $"Khách hàng {customer.FullName} đổi voucher {personalCode}.", ct);

        return new PoolHub.Core.DTOs.Admin.DiscountDto
        {
            DiscountId = personalVoucher.DiscountId,
            DiscountCode = personalVoucher.DiscountCode,
            Name = personalVoucher.Name,
            DiscountType = personalVoucher.DiscountType,
            Value = personalVoucher.Value,
            MaxAmount = personalVoucher.MaxAmount,
            MinTimeSubtotal = personalVoucher.MinTimeSubtotal,
            AppliesTo = personalVoucher.AppliesTo,
            StartsAtUtc = personalVoucher.StartsAtUtc,
            EndsAtUtc = personalVoucher.EndsAtUtc,
            IsActive = personalVoucher.IsActive,
            IsVoucher = personalVoucher.IsVoucher,
            PointsRequired = personalVoucher.PointsRequired,
            CustomerId = personalVoucher.CustomerId,
            CustomerName = customer.FullName,
            MaxUsage = personalVoucher.MaxUsage,
            UsageCount = personalVoucher.UsageCount
        };
    }

    public async Task<PagedResult<CustomerPointHistoryDto>> GetPointHistoryAsync(long customerId, PaginationRequest request, CancellationToken ct)
    {
        await EnsureCustomerExistsAsync(customerId, ct);
        NormalizePagination(request);
        var query = db.CustomerPointHistories.AsNoTracking().Where(x => x.CustomerId == customerId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new CustomerPointHistoryDto
            {
                CustomerPointHistoryId = x.CustomerPointHistoryId,
                CustomerId = x.CustomerId,
                Points = x.Points,
                TransactionType = x.TransactionType,
                Description = x.Description,
                ReferenceId = x.ReferenceId,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync(ct);
        return Page(items, request, total);
    }

    public async Task<CustomerPortalProfileDto> GetPortalProfileAsync(long userId, CancellationToken ct)
    {
        var customer = await GetPortalCustomerAsync(userId, ct);
        var account = await db.Users.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new { x.Email, x.PhoneNumber, x.FullName })
            .FirstOrDefaultAsync(ct);
        return new CustomerPortalProfileDto
        {
            CustomerId = customer.CustomerId,
            PublicId = customer.PublicId,
            FullName = string.IsNullOrWhiteSpace(customer.FullName)
                ? account?.FullName ?? string.Empty
                : customer.FullName,
            PhoneNumber = string.IsNullOrWhiteSpace(customer.PhoneNumber)
                ? account?.PhoneNumber ?? string.Empty
                : customer.PhoneNumber,
            Email = string.IsNullOrWhiteSpace(customer.Email)
                ? account?.Email
                : customer.Email,
            LoyaltyPoints = customer.LoyaltyPoints,
            TotalPointsEarned = customer.TotalPointsEarned,
            CreatedAtUtc = customer.CreatedAtUtc
        };
    }

    public async Task<CustomerPortalProfileDto> UpdatePortalProfileAsync(
        long userId, UpdateCustomerPortalProfileRequest request, CancellationToken ct)
    {
        var customer = await GetPortalCustomerAsync(userId, ct);
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User account not found.");
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException("Phone number is required.");
        if (await db.Customers.AnyAsync(
                x => x.CustomerId != customer.CustomerId && x.PhoneNumber == phone, ct))
            throw new ConflictException("Customer phone number already exists.");
        if (await db.Users.AnyAsync(
                x => x.UserId != userId && x.PhoneNumber == phone, ct))
            throw new ConflictException("Phone number is already used by another account.");

        var fullName = request.FullName.Trim();
        var oldValues = new { customer.FullName, customer.PhoneNumber };
        customer.FullName = fullName;
        customer.PhoneNumber = phone;
        customer.UpdatedAtUtc = _clock.UtcNow;
        user.FullName = fullName;
        user.PhoneNumber = phone;
        user.UpdatedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(userId, AuditActions.CustomerUpdated, nameof(CustomerEntity),
            customer.CustomerId, customer.PublicId, oldValues,
            new { customer.FullName, customer.PhoneNumber },
            "Customer updated their profile.", ct);
        return await GetPortalProfileAsync(userId, ct);
    }

    public async Task<IReadOnlyList<PoolHub.Core.DTOs.Admin.DiscountDto>> GetPortalVoucherTemplatesAsync(
        long userId, CancellationToken ct)
    {
        await ResolvePortalCustomerIdAsync(userId, ct);
        var now = _clock.UtcNow;
        return await db.Discounts.AsNoTracking()
            .Where(x => x.CustomerId == null && x.IsVoucher && x.IsActive &&
                        x.PointsRequired > 0 && x.StartsAtUtc <= now &&
                        (x.EndsAtUtc == null || x.EndsAtUtc > now))
            .OrderBy(x => x.PointsRequired)
            .ThenBy(x => x.Name)
            .Select(x => new PoolHub.Core.DTOs.Admin.DiscountDto
            {
                DiscountId = x.DiscountId,
                DiscountCode = x.DiscountCode,
                Name = x.Name,
                DiscountType = x.DiscountType,
                Value = x.Value,
                MaxAmount = x.MaxAmount,
                MinTimeSubtotal = x.MinTimeSubtotal,
                AppliesTo = x.AppliesTo,
                StartsAtUtc = x.StartsAtUtc,
                EndsAtUtc = x.EndsAtUtc,
                IsActive = x.IsActive,
                IsVoucher = x.IsVoucher,
                PointsRequired = x.PointsRequired,
                MaxUsage = x.MaxUsage,
                UsageCount = x.UsageCount
            })
            .ToListAsync(ct);
    }

    public async Task<PoolHub.Core.DTOs.Admin.DiscountDto> ExchangePortalVoucherAsync(
        long userId, long voucherTemplateId, CancellationToken ct) =>
        await ExchangeVoucherAsync(
            await ResolvePortalCustomerIdAsync(userId, ct),
            voucherTemplateId,
            userId,
            ct);

    public async Task<PagedResult<CustomerBookingHistoryDto>> GetPortalBookingHistoryAsync(
        long userId, PaginationRequest request, CancellationToken ct) =>
        await GetBookingHistoryAsync(await ResolvePortalCustomerIdAsync(userId, ct), request, ct);

    public async Task<PagedResult<CustomerSessionHistoryDto>> GetPortalSessionHistoryAsync(
        long userId, PaginationRequest request, CancellationToken ct) =>
        await GetSessionHistoryAsync(await ResolvePortalCustomerIdAsync(userId, ct), request, ct);

    public async Task<PagedResult<CustomerInvoiceHistoryDto>> GetPortalInvoiceHistoryAsync(
        long userId, PaginationRequest request, CancellationToken ct) =>
        await GetInvoiceHistoryAsync(await ResolvePortalCustomerIdAsync(userId, ct), request, ct);

    public async Task<PagedResult<PoolHub.Core.DTOs.Admin.DiscountDto>> GetPortalVouchersAsync(
        long userId, PaginationRequest request, CancellationToken ct)
    {
        var customerId = await ResolvePortalCustomerIdAsync(userId, ct);
        NormalizePagination(request);
        var query = db.Discounts.AsNoTracking()
            // A customer id marks a personal voucher. Redeemed vouchers created
            // by older data may have a null points-required value, so do not
            // hide them by requiring the newer zero sentinel.
            .Where(x => x.CustomerId == customerId && x.IsVoucher);
        var total = await query.CountAsync(ct);
        var customerName = await db.Customers.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .Select(x => x.FullName)
            .FirstOrDefaultAsync(ct);
        var items = await query
            .OrderByDescending(x => x.DiscountId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PoolHub.Core.DTOs.Admin.DiscountDto
            {
                DiscountId = x.DiscountId,
                DiscountCode = x.DiscountCode,
                Name = x.Name,
                DiscountType = x.DiscountType,
                Value = x.Value,
                MaxAmount = x.MaxAmount,
                MinTimeSubtotal = x.MinTimeSubtotal,
                AppliesTo = x.AppliesTo,
                StartsAtUtc = x.StartsAtUtc,
                EndsAtUtc = x.EndsAtUtc,
                IsActive = x.IsActive,
                IsVoucher = x.IsVoucher,
                PointsRequired = x.PointsRequired,
                CustomerId = x.CustomerId,
                CustomerName = customerName,
                MaxUsage = x.MaxUsage,
                UsageCount = x.UsageCount
            })
            .ToListAsync(ct);
        return Page(items, request, total);
    }

    public async Task<PagedResult<CustomerPointHistoryDto>> GetPortalPointHistoryAsync(
        long userId, PaginationRequest request, CancellationToken ct) =>
        await GetPointHistoryAsync(await ResolvePortalCustomerIdAsync(userId, ct), request, ct);

    public async Task<long> ResolvePortalCustomerIdAsync(long userId, CancellationToken ct) =>
        (await GetPortalCustomerAsync(userId, ct)).CustomerId;

    private Task<bool> CustomerExistsAsync(long id, CancellationToken ct) =>
        db.Customers.AsNoTracking().AnyAsync(x => x.CustomerId == id, ct);

    private async Task<CustomerEntity> GetPortalCustomerAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new NotFoundException("User account not found.");

        var customer = await db.Customers
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (customer is null)
        {
            var normalizedPhone = PhoneNumberNormalizer.Normalize(user.PhoneNumber);
            var normalizedEmail = NormalizeEmail(user.Email);
            var byEmail = string.IsNullOrWhiteSpace(normalizedEmail)
                ? null
                : await db.Customers.FirstOrDefaultAsync(
                    x => x.Email != null && x.Email.ToLower() == normalizedEmail,
                    ct);
            var byPhone = string.IsNullOrWhiteSpace(normalizedPhone)
                ? null
                : await db.Customers.FirstOrDefaultAsync(x => x.PhoneNumber == normalizedPhone, ct);

            if (byEmail is not null && byPhone is not null && byEmail.CustomerId != byPhone.CustomerId)
                throw new ConflictException("Customer profile matching this account is ambiguous.");

            customer = byEmail ?? byPhone;
            if (customer is null)
            {
                if (string.IsNullOrWhiteSpace(normalizedPhone))
                    throw new NotFoundException("Customer profile is not linked to this account yet.");

                customer = new CustomerEntity
                {
                    FullName = user.FullName,
                    PhoneNumber = normalizedPhone,
                    Email = user.Email,
                    Status = true,
                    UserId = userId
                };
                db.Customers.Add(customer);
            }
            else
            {
                if (!customer.Status)
                    throw new ForbiddenException("Customer profile is inactive.");
                if (customer.UserId.HasValue && customer.UserId.Value != userId)
                    throw new ConflictException("Customer profile is linked to another account.");
                if (!string.IsNullOrWhiteSpace(customer.Email) &&
                    !string.Equals(customer.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("Customer profile matching this account is ambiguous.");
                }
                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber) &&
                    !string.IsNullOrWhiteSpace(normalizedPhone) &&
                    !string.Equals(
                        PhoneNumberNormalizer.Normalize(customer.PhoneNumber),
                        normalizedPhone,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("Customer profile matching this account is ambiguous.");
                }
                customer.UserId = userId;
            }

            await db.SaveChangesAsync(ct);
        }

        if (!customer.Status)
            throw new ForbiddenException("Customer profile is inactive.");
        return customer;
    }

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
