using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PoolHub.Core.DTOs.Booking;
using PoolHub.Core.DTOs.Common;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Payments;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using EntityBooking = PoolHub.Core.Entities.Booking;
using EntityCustomer = PoolHub.Core.Entities.Customer;

namespace PoolHub.Services.Booking;

[Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
public class BookingService(PoolHubDbContext db, IEmailService emailService, ILogger<BookingService> logger, IPosNotificationService posNotificationService, IBookingDepositRefundService? refundService = null, IClock? clock = null, IConfiguration? config = null, IHttpClientFactory? httpClientFactory = null) : IBookingService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;
    private IBookingDepositRefundService RefundService => refundService ?? new BookingDepositRefundService(db, null, _clock);
    private const int BookingDepositPercent = 30;
    private const decimal MinimumDepositAmount = 50000m;
    private const int DepositHoldMinutes = 10;
    private const int NoShowGraceMinutes = 15;
    private const int CancellationRefundHours = 2;
    private const int MaxActiveBookingsPerPhonePerDay = 2;

    public BookingService(PoolHubDbContext db, IEmailService emailService, ILogger<BookingService> logger)
        : this(db, emailService, logger, new NoOpPosNotificationService(), null, null, null, null)
    {
    }

    public async Task<PagedResult<BookingDto>> GetBookingsAsync(BookingQueryRequest request, CancellationToken ct)
    {
        await ApplyAutomaticBookingStatusesAsync(_clock.UtcNow, ct);
        var query = db.Bookings.AsNoTracking().AsQueryable();

        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.Date.HasValue)
        {
            var (fromUtc, toUtc) = BusinessTime.LocalDateRangeToUtc(request.Date.Value);
            query = query.Where(x => x.StartTimeUtc >= fromUtc && x.StartTimeUtc < toUtc);
        }
        if (request.TableId.HasValue)
        {
            query = query.Where(x => x.TableId == request.TableId.Value ||
                                     db.BookingTables.Any(bt => bt.BookingId == x.BookingId && bt.TableId == request.TableId.Value));
        }

        var total = await query.CountAsync(ct);
        var bookings = await query
            .OrderByDescending(x => x.BookingId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var items = new List<BookingDto>();
        foreach (var booking in bookings)
        {
            items.Add(await MapAsync(booking, ct));
        }

        return new PagedResult<BookingDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<BookingDto> GetByIdAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status == BookingStatuses.PendingDeposit)
        {
            await CheckAndConfirmDepositFromPayOsAsync(booking, ct);
        }
        if (await ApplyAutomaticBookingStatusAsync(booking, _clock.UtcNow, ct))
        {
            await db.SaveChangesAsync(ct);
        }

        return await MapAsync(booking, ct);
    }

    public Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken ct) =>
        CreateInternalAsync(request, "Staff", forceApprovalForLargeBooking: false, ct);

    public Task<BookingDto> CreatePublicAsync(CreateBookingRequest request, CancellationToken ct) =>
        CreateInternalAsync(request, "Public", forceApprovalForLargeBooking: true, ct);

    public async Task<BookingDto> UpdateAsync(long id, UpdateBookingRequest request, CancellationToken ct)
    {
        ValidateBookingPeriod(request.StartTimeUtc, request.EndTimeUtc);
        var now = _clock.UtcNow;
        if (request.EndTimeUtc <= now) throw new BusinessRuleException("Booking end time must be in the future.");
        if (request.NumberOfGuests < 1 || request.NumberOfGuests > 20) throw new ValidationException("Number of guests must be between 1 and 20.");

        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status is BookingStatuses.Cancelled or BookingStatuses.NoShow or BookingStatuses.Completed or BookingStatuses.Expired)
            throw new BusinessRuleException("Booking cannot be updated.");
        if (await db.Sessions.AnyAsync(x => x.BookingId == id, ct))
            throw new ConflictException("Booking already has a session and cannot be changed.");

        var tableIds = NormalizeTableIds(request.TableIds, request.TableId);
        if (tableIds.Count == 0) throw new ValidationException("At least one table must be selected.");
        await EnsureTablesCanBeBookedAsync(tableIds, booking.BookingId, request.StartTimeUtc, request.EndTimeUtc, ct);

        var estimated = await EstimateAmountAsync(tableIds, request.TableTypeId, request.StartTimeUtc, request.EndTimeUtc, ct);
        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);

        booking.TableId = tableIds.FirstOrDefault();
        booking.TableTypeId = request.TableTypeId;
        booking.StartTimeUtc = request.StartTimeUtc;
        booking.EndTimeUtc = request.EndTimeUtc;
        booking.NumberOfGuests = request.NumberOfGuests;
        booking.Note = request.Note?.Trim();
        booking.EstimatedAmount = estimated;
        booking.UpdatedAtUtc = now;

        await ReplaceBookingTablesAsync(booking.BookingId, tableIds, ct);
        if (deposit is not null && deposit.Status == BookingDepositStatuses.Pending)
        {
            deposit.RequiredAmount = CalculateDepositRequiredAmount(estimated);
            deposit.DueAtUtc = booking.HoldExpiresAtUtc ?? now.AddMinutes(DepositHoldMinutes);
        }

        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> ConfirmAsync(long id, long? confirmedByUserId, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        var now = _clock.UtcNow;
        if (await ApplyAutomaticBookingStatusAsync(booking, now, ct))
        {
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Booking has expired and cannot be confirmed.");
        }

        if (booking.Status is not (BookingStatuses.Pending or BookingStatuses.PendingDeposit))
            throw new BusinessRuleException("Only pending bookings can be confirmed.");
        if (await HasConflictAsync(booking.BookingId, await GetBookingTableIdsAsync(booking, ct), booking.StartTimeUtc, booking.EndTimeUtc, ct))
            throw new ConflictException("Table is already booked for the selected time.");

        booking.Status = BookingStatuses.Confirmed;
        booking.ConfirmedByUserId = confirmedByUserId;
        booking.ConfirmedAtUtc ??= now;
        await CreateBookingConfirmedNotificationAsync(booking, ct);
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        await TrySendBookingConfirmedEmailAsync(booking, ct);

        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> ApproveAsync(long id, long approvedByUserId, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != BookingStatuses.PendingApproval) throw new BusinessRuleException("Only PendingApproval bookings can be approved.");

        var now = _clock.UtcNow;
        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        booking.Status = BookingStatuses.PendingDeposit;
        booking.RequiresApproval = false;
        booking.ApprovedByUserId = approvedByUserId;
        booking.ApprovedAtUtc = now;
        booking.HoldExpiresAtUtc = now.AddMinutes(DepositHoldMinutes);
        booking.UpdatedAtUtc = now;

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
        if (deposit is null)
        {
            db.BookingDeposits.Add(NewDeposit(booking.BookingId, booking.EstimatedAmount, booking.HoldExpiresAtUtc.Value));
        }
        else
        {
            deposit.Status = BookingDepositStatuses.Pending;
            deposit.DueAtUtc = booking.HoldExpiresAtUtc.Value;
            deposit.RequiredAmount = CalculateDepositRequiredAmount(booking.EstimatedAmount);
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> MockPayDepositAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (await ApplyAutomaticBookingStatusAsync(booking, _clock.UtcNow, ct))
        {
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Booking deposit hold has expired.");
        }

        if (booking.Status != BookingStatuses.PendingDeposit) throw new BusinessRuleException("Only PendingDeposit bookings can pay deposit.");
        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == id, ct)
            ?? throw new NotFoundException("Booking deposit not found.");

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        var now = _clock.UtcNow;
        deposit.Status = BookingDepositStatuses.Paid;
        deposit.PaidAmount = deposit.RequiredAmount;
        deposit.PaidAtUtc = now;
        deposit.TransactionCode = $"DEP{now:yyyyMMddHHmmss}{id}";
        booking.Status = BookingStatuses.Confirmed;
        booking.ConfirmedAtUtc ??= now;
        booking.UpdatedAtUtc = now;
        await CreateBookingConfirmedNotificationAsync(booking, ct);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        await TrySendBookingConfirmedEmailAsync(booking, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> SubmitDepositTransferAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status == BookingStatuses.PendingDeposit)
        {
            await CheckAndConfirmDepositFromPayOsAsync(booking, ct);
            if (booking.Status == BookingStatuses.Confirmed)
            {
                return await MapAsync(booking, ct);
            }
        }

        var now = _clock.UtcNow;
        if (await ApplyAutomaticBookingStatusAsync(booking, now, ct))
        {
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Booking deposit hold has expired.");
        }

        if (booking.Status != BookingStatuses.PendingDeposit)
            throw new BusinessRuleException("Only PendingDeposit bookings can submit deposit transfer.");

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == id, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        if (deposit.Status != BookingDepositStatuses.Pending)
            throw new BusinessRuleException("Only pending deposits can be submitted for verification.");

        deposit.Status = BookingDepositStatuses.PendingVerification;
        booking.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> ConfirmDepositAsync(long id, ConfirmDepositRequest request, long confirmedByUserId, CancellationToken ct)
    {
        if (request.PaidAmount <= 0) throw new ValidationException("Paid amount is required.");
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        var now = _clock.UtcNow;
        if (await ApplyAutomaticBookingStatusAsync(booking, now, ct))
        {
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Booking deposit hold has expired.");
        }

        if (booking.Status != BookingStatuses.PendingDeposit)
            throw new BusinessRuleException("Only PendingDeposit bookings can confirm deposit.");

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == id, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        if (deposit.Status is not (BookingDepositStatuses.Pending or BookingDepositStatuses.PendingVerification))
            throw new BusinessRuleException("Only pending deposits can be confirmed.");
        if (request.PaidAmount < deposit.RequiredAmount)
            throw new BusinessRuleException("Paid amount must be at least the required deposit amount.");

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        deposit.Status = BookingDepositStatuses.Paid;
        deposit.PaidAmount = request.PaidAmount;
        deposit.PaidAtUtc = now;
        deposit.TransactionCode = string.IsNullOrWhiteSpace(request.TransactionCode)
            ? $"MANUAL{now:yyyyMMddHHmmss}{id}"
            : request.TransactionCode.Trim();
        booking.Status = BookingStatuses.Confirmed;
        booking.ConfirmedByUserId = confirmedByUserId;
        booking.ConfirmedAtUtc ??= now;
        booking.UpdatedAtUtc = now;
        await CreateBookingConfirmedNotificationAsync(booking, ct);
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        await TrySendBookingConfirmedEmailAsync(booking, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> RejectDepositTransferAsync(long id, RejectDepositTransferRequest request, long rejectedByUserId, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        var now = _clock.UtcNow;
        if (await ApplyAutomaticBookingStatusAsync(booking, now, ct))
        {
            await db.SaveChangesAsync(ct);
            throw new ConflictException("Booking deposit hold has expired.");
        }

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == id, ct)
            ?? throw new NotFoundException("Booking deposit not found.");
        if (booking.Status != BookingStatuses.PendingDeposit || deposit.Status != BookingDepositStatuses.PendingVerification)
            throw new BusinessRuleException("Only deposits pending verification can be rejected.");

        deposit.Status = BookingDepositStatuses.Pending;
        booking.Note = AppendAutomaticNote(booking.Note, $"Deposit verification rejected by user #{rejectedByUserId}: {request.Reason?.Trim() ?? "No reason provided."}");
        booking.UpdatedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> CancelAsync(long id, CancelBookingRequest request, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status is BookingStatuses.Cancelled or BookingStatuses.Completed or BookingStatuses.NoShow or BookingStatuses.Expired)
            throw new BusinessRuleException("Booking cannot be cancelled.");

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        var now = _clock.UtcNow;
        booking.Status = BookingStatuses.Cancelled;
        booking.CancelledAtUtc = now;
        booking.CancellationReason = request.Reason?.Trim();
        booking.UpdatedAtUtc = now;

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
        if (deposit is not null && deposit.Status == BookingDepositStatuses.Paid)
        {
            var refund = request.CancelledByVenue || booking.StartTimeUtc - now >= TimeSpan.FromHours(CancellationRefundHours);
            if (refund)
            {
                if (request.CancelledByVenue)
                {
                    await RefundService.CreateVenueFaultRefundAsync(deposit.BookingDepositId, null, ct);
                }
                else
                {
                    await RefundService.CreateEligibleCancellationRefundAsync(deposit.BookingDepositId, null, ct);
                }
            }
            else
            {
                deposit.Status = BookingDepositStatuses.Forfeited;
                deposit.ForfeitedAmount = deposit.PaidAmount;
                deposit.ForfeitedAtUtc = now;
            }
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        await TrySendBookingCancelledEmailAsync(booking, request.Reason ?? "Đặt bàn đã bị hủy.", ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> MarkNoShowAsync(long id, NoShowBookingRequest request, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != BookingStatuses.Confirmed) throw new BusinessRuleException("Only Confirmed bookings can be marked NoShow.");
        var now = _clock.UtcNow;
        if (now < booking.StartTimeUtc.AddMinutes(NoShowGraceMinutes))
            throw new BusinessRuleException("No-show grace period has not elapsed.");

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        booking.Status = BookingStatuses.NoShow;
        booking.NoShowAtUtc = now;
        booking.Note = AppendAutomaticNote(booking.Note, request.Reason ?? "Marked no-show.");
        booking.UpdatedAtUtc = now;

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
        if (deposit is not null && deposit.Status == BookingDepositStatuses.Paid)
        {
            deposit.Status = BookingDepositStatuses.Forfeited;
            deposit.ForfeitedAmount = deposit.PaidAmount;
            deposit.ForfeitedAtUtc = now;
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<BookingDto> MarkCompletedAsync(long id, CancellationToken ct)
    {
        var booking = await db.Bookings.FindAsync([id], ct) ?? throw new NotFoundException("Booking not found.");
        if (booking.Status != BookingStatuses.Confirmed) throw new BusinessRuleException("Only Confirmed bookings can be completed.");
        booking.Status = BookingStatuses.Completed;
        booking.UpdatedAtUtc = _clock.UtcNow;
        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        return await MapAsync(booking, ct);
    }

    public async Task<List<AvailableTableDto>> GetAvailabilityAsync(BookingAvailabilityRequest request, CancellationToken ct)
    {
        await ExpirePendingDepositsAsync(_clock.UtcNow, ct);
        ValidateBookingPeriod(request.StartTimeUtc, request.EndTimeUtc);

        var requestedTableIds = NormalizeTableIds(request.TableIds, request.TableId);
        var activeSessionTableIds = await GetActiveSessionTableIdsAsync(requestedTableIds, ct);

        var query = from table in db.VenueTables.AsNoTracking()
                    join type in db.TableTypes.AsNoTracking() on table.TableTypeId equals type.TableTypeId
                    where table.IsActive
                       && table.OperationalStatus != 4
                       && table.OperationalStatus != 5
                    select new { table, type };
        if (requestedTableIds.Count > 0) query = query.Where(x => requestedTableIds.Contains(x.table.TableId));
        if (request.TableTypeId.HasValue) query = query.Where(x => x.table.TableTypeId == request.TableTypeId.Value);

        var rows = await query.OrderBy(x => x.table.TableCode).ToListAsync(ct);
        var result = new List<AvailableTableDto>();
        foreach (var row in rows)
        {
            if (activeSessionTableIds.Contains(row.table.TableId)) continue;
            var conflict = await HasConflictAsync(0, [row.table.TableId], request.StartTimeUtc, request.EndTimeUtc, ct);
            var estimated = await EstimateAmountAsync([row.table.TableId], row.table.TableTypeId, request.StartTimeUtc, request.EndTimeUtc, ct);
            if (!conflict)
            {
                result.Add(new AvailableTableDto
                {
                    TableId = row.table.TableId,
                    TableCode = row.table.TableCode,
                    TableName = row.table.TableName,
                    TableTypeId = row.table.TableTypeId,
                    TableTypeName = row.type.Name,
                    Capacity = row.table.Capacity,
                    EstimatedAmount = estimated,
                    DepositRequiredAmount = CalculateDepositRequiredAmount(estimated),
                    DepositPercent = BookingDepositPercent,
                    RequiresApproval = false
                });
            }
        }

        return result;
    }

    public async Task<PagedResult<BookingCalendarItem>> GetCalendarAsync(BookingCalendarRequest request, CancellationToken ct)
    {
        ValidateCalendarRequest(request);
        await ApplyAutomaticBookingStatusesAsync(_clock.UtcNow, ct);

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var query = db.Bookings.AsNoTracking().Where(b => b.StartTimeUtc < request.To && b.EndTimeUtc > request.From);
        if (request.TableId.HasValue) query = query.Where(b => b.TableId == request.TableId.Value || db.BookingTables.Any(bt => bt.BookingId == b.BookingId && bt.TableId == request.TableId.Value));
        if (request.Status.HasValue) query = query.Where(b => b.Status == request.Status.Value);

        var total = await query.CountAsync(ct);
        var bookings = await query.OrderBy(b => b.StartTimeUtc).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = new List<BookingCalendarItem>();
        foreach (var b in bookings)
        {
            items.Add(new BookingCalendarItem
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                CustomerId = b.CustomerId,
                CustomerName = await db.Customers.Where(c => c.CustomerId == b.CustomerId).Select(c => c.FullName).FirstOrDefaultAsync(ct) ?? string.Empty,
                CustomerPhone = await db.Customers.Where(c => c.CustomerId == b.CustomerId).Select(c => c.PhoneNumber).FirstOrDefaultAsync(ct) ?? string.Empty,
                TableId = b.TableId,
                TableCode = b.TableId.HasValue ? await db.VenueTables.Where(t => t.TableId == b.TableId.Value).Select(t => t.TableCode).FirstOrDefaultAsync(ct) : null,
                TableName = b.TableId.HasValue ? await db.VenueTables.Where(t => t.TableId == b.TableId.Value).Select(t => t.TableName).FirstOrDefaultAsync(ct) : null,
                TableTypeId = b.TableTypeId,
                TableTypeName = b.TableTypeId.HasValue ? await db.TableTypes.Where(tt => tt.TableTypeId == b.TableTypeId.Value).Select(tt => tt.Name).FirstOrDefaultAsync(ct) : null,
                StartTimeUtc = b.StartTimeUtc,
                EndTimeUtc = b.EndTimeUtc,
                NumberOfGuests = b.NumberOfGuests,
                Status = b.Status,
                Note = b.Note,
                HasSession = await db.Sessions.AnyAsync(s => s.BookingId == b.BookingId, ct),
                ConfirmedAtUtc = b.ConfirmedAtUtc,
                CancelledAtUtc = b.CancelledAtUtc
            });
        }

        return new PagedResult<BookingCalendarItem> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IEnumerable<PublicBookingSlotDto>> GetPublicCalendarAsync(long tableId, DateTime date, CancellationToken ct)
    {
        var nowUtc = _clock.UtcNow;
        await ApplyAutomaticBookingStatusesAsync(nowUtc, ct);
        var (startOfDay, endOfDay) = BusinessTime.LocalDateRangeToUtc(date);
        return await db.Bookings
            .Where(b => (b.TableId == tableId || db.BookingTables.Any(bt => bt.BookingId == b.BookingId && bt.TableId == tableId)) &&
                        b.StartTimeUtc < endOfDay &&
                        b.EndTimeUtc > startOfDay &&
                        (b.Status == BookingStatuses.Confirmed ||
                         b.Status == BookingStatuses.PendingApproval ||
                         (b.Status == BookingStatuses.PendingDeposit &&
                          (b.HoldExpiresAtUtc == null || b.HoldExpiresAtUtc > nowUtc))))
            .OrderBy(b => b.StartTimeUtc)
            .Select(b => new PublicBookingSlotDto { StartTimeUtc = b.StartTimeUtc, EndTimeUtc = b.EndTimeUtc, Status = b.Status })
            .ToListAsync(ct);
    }

    public async Task<int> ExpirePendingDepositsAsync(DateTime nowUtc, CancellationToken ct)
    {
        var bookings = await db.Bookings
            .Where(x => x.Status == BookingStatuses.PendingDeposit && x.HoldExpiresAtUtc != null && x.HoldExpiresAtUtc <= nowUtc)
            .ToListAsync(ct);
        foreach (var booking in bookings)
        {
            booking.Status = BookingStatuses.Expired;
            booking.UpdatedAtUtc = nowUtc;
            var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
            if (deposit is not null && deposit.Status is BookingDepositStatuses.Pending or BookingDepositStatuses.PendingVerification)
            {
                deposit.Status = BookingDepositStatuses.Expired;
            }
        }

        if (bookings.Count > 0) await db.SaveChangesAsync(ct);
        return bookings.Count;
    }

    private async Task<BookingDto> CreateInternalAsync(CreateBookingRequest request, string source, bool forceApprovalForLargeBooking, CancellationToken ct)
    {
        ValidateBookingPeriod(request.StartTimeUtc, request.EndTimeUtc);
        if (!IsAlignedToThirtyMinutes(request.StartTimeUtc) || !IsAlignedToThirtyMinutes(request.EndTimeUtc))
            throw new BusinessRuleException("Thời gian đặt bàn phải là các mốc chẵn 30 phút (VD: 10:00, 10:30).");

        var customerId = await ResolveCustomerIdAsync(request, ct);
        await EnsurePhoneActiveBookingLimitAsync(customerId, request.StartTimeUtc, ct);

        var tableIds = NormalizeTableIds(request.TableIds, request.TableId);
        if (tableIds.Count == 0) throw new ValidationException("At least one table must be selected.");
        await EnsureTablesCanBeBookedAsync(tableIds, 0, request.StartTimeUtc, request.EndTimeUtc, ct);

        var estimated = await EstimateAmountAsync(tableIds, request.TableTypeId, request.StartTimeUtc, request.EndTimeUtc, ct);
        var now = _clock.UtcNow;
        var status = BookingStatuses.PendingDeposit;
        var holdExpiresAtUtc = now.AddMinutes(DepositHoldMinutes);
        if (source == "Public")
        {
            _ = await GetBankTransferQrConfigAsync(required: true, ct);
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        var entity = new EntityBooking
        {
            CustomerId = customerId,
            TableId = tableIds[0],
            TableTypeId = request.TableTypeId,
            BookingCode = $"BK{now:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            NumberOfGuests = request.NumberOfGuests,
            Status = status,
            EstimatedAmount = estimated,
            RequiresApproval = false,
            HoldExpiresAtUtc = holdExpiresAtUtc,
            Source = source,
            Note = request.Note?.Trim()
        };
        db.Bookings.Add(entity);
        await db.SaveChangesAsync(ct);
        db.BookingTables.AddRange(tableIds.Select(tableId => new PoolHub.Core.Entities.BookingTable { BookingId = entity.BookingId, TableId = tableId }));
        db.BookingDeposits.Add(NewDeposit(entity.BookingId, estimated, holdExpiresAtUtc));
        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        await posNotificationService.NotifyBookingUpdateAsync((int)entity.BookingId, ct);
        return await MapAsync(entity, ct);
    }

    private async Task<long> ResolveCustomerIdAsync(CreateBookingRequest request, CancellationToken ct)
    {
        var phone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var email = NormalizeEmail(request.Email);

        if (request.CustomerId.HasValue && request.CustomerId > 0)
        {
            var requestedCustomer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId.Value, ct)
                ?? throw new NotFoundException("Customer not found.");
            if (!requestedCustomer.Status) throw new BusinessRuleException("Customer is blocked or inactive.");
            return requestedCustomer.CustomerId;
        }

        if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(email))
            throw new ValidationException("Either CustomerId or PhoneNumber must be provided.");

        var existing = await db.Customers.FirstOrDefaultAsync(c =>
            (!string.IsNullOrEmpty(phone) && c.PhoneNumber == phone) ||
            (!string.IsNullOrEmpty(email) && c.Email == email), ct);
        if (existing is not null)
        {
            if (!existing.Status) throw new BusinessRuleException("Customer is blocked or inactive.");
            if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(existing.Email)) existing.Email = email;
            if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(existing.PhoneNumber)) existing.PhoneNumber = phone;
            await db.SaveChangesAsync(ct);
            return existing.CustomerId;
        }

        var customer = new EntityCustomer { PhoneNumber = phone, FullName = request.CustomerName?.Trim() ?? "Anonymous", Email = email };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer.CustomerId;
    }

    private async Task EnsurePhoneActiveBookingLimitAsync(long customerId, DateTime startTimeUtc, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == customerId, ct);
        if (customer is null || string.IsNullOrWhiteSpace(customer.PhoneNumber)) return;
        var localDate = BusinessTime.UtcToVietnamLocalDate(startTimeUtc);
        var (dayStart, dayEnd) = BusinessTime.LocalDateRangeToUtc(localDate);
        var nowUtc = _clock.UtcNow;
        var active = await db.Bookings.CountAsync(b =>
            b.CustomerId == customerId &&
            b.StartTimeUtc < dayEnd &&
            b.EndTimeUtc > dayStart &&
            (b.Status == BookingStatuses.Confirmed ||
             b.Status == BookingStatuses.PendingApproval ||
             (b.Status == BookingStatuses.PendingDeposit &&
              (b.HoldExpiresAtUtc == null || b.HoldExpiresAtUtc > nowUtc))), ct);
        if (active >= MaxActiveBookingsPerPhonePerDay)
            throw new BusinessRuleException("Phone number has too many active bookings on the selected day.");
    }

    private async Task EnsureTablesCanBeBookedAsync(List<long> tableIds, long excludeBookingId, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken ct)
    {
        if (tableIds.Count == 0) return;
        var tables = await db.VenueTables.Where(x => tableIds.Contains(x.TableId)).ToListAsync(ct);
        var missing = tableIds.Except(tables.Select(x => x.TableId)).ToList();
        if (missing.Count > 0) throw new NotFoundException($"Table not found: {string.Join(", ", missing)}.");
        var unavailable = tables.FirstOrDefault(x => !x.IsActive || x.OperationalStatus is 4 or 5);
        if (unavailable is not null) throw new BusinessRuleException($"Table is not available for booking (Status: {unavailable.OperationalStatus}).");
        var activeSessionTableIds = await GetActiveSessionTableIdsAsync(tableIds, ct);
        if (activeSessionTableIds.Count > 0)
        {
            var activeTableLabels = tables
                .Where(x => activeSessionTableIds.Contains(x.TableId))
                .OrderBy(x => x.TableCode)
                .Select(x => string.IsNullOrWhiteSpace(x.TableCode) ? x.TableName : x.TableCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            var details = activeTableLabels.Count > 0
                ? $" Active tables: {string.Join(", ", activeTableLabels)}."
                : string.Empty;
            throw new ConflictException($"One or more tables are currently in use and cannot be booked.{details}");
        }
        if (await HasConflictAsync(excludeBookingId, tableIds, startTimeUtc, endTimeUtc, ct))
            throw new ConflictException("Table is already booked for the selected time.");
    }

    private async Task<HashSet<long>> GetActiveSessionTableIdsAsync(IReadOnlyCollection<long> tableIds, CancellationToken ct)
    {
        var query = from assignment in db.SessionTableAssignments.AsNoTracking()
                    join session in db.Sessions.AsNoTracking() on assignment.SessionId equals session.SessionId
                    where assignment.EndedAtUtc == null
                       && session.Status == 1
                    select assignment.TableId;
        if (tableIds.Count > 0) query = query.Where(tableId => tableIds.Contains(tableId));
        return (await query.ToListAsync(ct)).ToHashSet();
    }

    private async Task<bool> HasConflictAsync(long excludeBookingId, List<long> tableIds, DateTime startTimeUtc, DateTime endTimeUtc, CancellationToken ct)
    {
        if (tableIds.Count == 0) return false;
        var nowUtc = _clock.UtcNow;
        return await db.Bookings.AnyAsync(b =>
            b.BookingId != excludeBookingId &&
            (tableIds.Contains(b.TableId ?? 0) || db.BookingTables.Any(bt => bt.BookingId == b.BookingId && tableIds.Contains(bt.TableId))) &&
            (b.Status == BookingStatuses.Confirmed ||
             b.Status == BookingStatuses.PendingApproval ||
             (b.Status == BookingStatuses.PendingDeposit &&
              (b.HoldExpiresAtUtc == null || b.HoldExpiresAtUtc > nowUtc))) &&
            b.StartTimeUtc < endTimeUtc &&
            b.EndTimeUtc > startTimeUtc, ct);
    }

    private async Task<decimal> EstimateAmountAsync(List<long> tableIds, long? tableTypeId, DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        var durationMinutes = Math.Max(1, (int)Math.Ceiling((endUtc - startUtc).TotalMinutes));
        var ids = tableIds.Count > 0 ? tableIds : [];
        if (ids.Count == 0 && tableTypeId.HasValue)
        {
            var fallbackRate = await GetHourlyRateAsync(tableTypeId.Value, startUtc, ct);
            return CalculateTimeAmount(fallbackRate.HourlyRate, durationMinutes, fallbackRate.MinimumMinutes, fallbackRate.BillingBlockMinutes);
        }

        decimal total = 0;
        var tables = await db.VenueTables.AsNoTracking().Where(x => ids.Contains(x.TableId)).ToListAsync(ct);
        foreach (var table in tables)
        {
            var rate = await GetHourlyRateAsync(table.TableTypeId, startUtc, ct);
            total += CalculateTimeAmount(rate.HourlyRate, durationMinutes, rate.MinimumMinutes, rate.BillingBlockMinutes);
        }

        return total;
    }

    private async Task<(decimal HourlyRate, int MinimumMinutes, int BillingBlockMinutes)> GetHourlyRateAsync(long tableTypeId, DateTime startUtc, CancellationToken ct)
    {
        startUtc = BusinessTime.NormalizeUtc(startUtc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(startUtc, BusinessTime.TimeZone);
        var dayType = await GetDayTypeAsync(localTime.Date, ct);
        var previousDayType = await GetDayTypeAsync(localTime.Date.AddDays(-1), ct);
        var time = localTime.TimeOfDay;
        var activePlanIds = await db.PricingPlans
            .Where(x => x.IsActive && x.StartsAtUtc <= startUtc && (x.EndsAtUtc == null || x.EndsAtUtc >= startUtc))
            .OrderByDescending(x => x.IsDefault)
            .Select(x => x.PricingPlanId)
            .ToListAsync(ct);
        var rules = await db.PricingPlanRules
            .Where(x => activePlanIds.Contains(x.PricingPlanId) && x.TableTypeId == tableTypeId && x.IsActive && (x.DayType == dayType || x.DayType == previousDayType))
            .ToListAsync(ct);
        
        var orderedRules = rules.OrderBy(x => activePlanIds.IndexOf(x.PricingPlanId)).ToList();
        
        var rule = orderedRules.FirstOrDefault(x => RuleMatchesLocalTime(x.DayType, x.StartTime, x.EndTime, dayType, previousDayType, time))
            ?? orderedRules.FirstOrDefault()
            ?? await db.PricingPlanRules.AsNoTracking().Where(x => x.TableTypeId == tableTypeId && x.IsActive).OrderByDescending(x => x.PricingPlanRuleId).FirstOrDefaultAsync(ct);

        return rule is null ? (50000m, 0, 0) : (rule.HourlyRate, rule.MinimumMinutes, rule.BillingBlockMinutes);
    }

    private static decimal CalculateTimeAmount(decimal hourlyRate, int durationMinutes, int minimumMinutes, int billingBlockMinutes)
    {
        return Math.Round(((decimal)Math.Max(0, durationMinutes) / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateDepositRequiredAmount(decimal estimatedAmount)
    {
        var raw = Math.Max(estimatedAmount * BookingDepositPercent / 100m, MinimumDepositAmount);
        return Math.Ceiling(raw / 1000m) * 1000m;
    }

    private static PoolHub.Core.Entities.BookingDeposit NewDeposit(long bookingId, decimal estimatedAmount, DateTime dueAtUtc) => new()
    {
        BookingId = bookingId,
        RequiredAmount = CalculateDepositRequiredAmount(estimatedAmount),
        Status = BookingDepositStatuses.Pending,
        DueAtUtc = dueAtUtc
    };

    private async Task<BookingDto> MapAsync(EntityBooking booking, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == booking.CustomerId, ct);
        var tableIds = await GetBookingTableIdsAsync(booking, ct);
        var deposit = await db.BookingDeposits.AsNoTracking().FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
        var instruction = deposit is null ? null : await BuildDepositPaymentInstructionAsync(booking, customer?.PhoneNumber ?? string.Empty, deposit, ct);
        var refundSummary = deposit is null ? null : await RefundService.GetSummaryForBookingAsync(booking.BookingId, ct);
        return new BookingDto
        {
            BookingId = booking.BookingId,
            BookingCode = booking.BookingCode,
            CustomerId = booking.CustomerId,
            CustomerName = customer?.FullName ?? string.Empty,
            PhoneNumber = customer?.PhoneNumber ?? string.Empty,
            TableId = booking.TableId,
            TableIds = tableIds,
            TableTypeId = booking.TableTypeId,
            StartTimeUtc = booking.StartTimeUtc,
            EndTimeUtc = booking.EndTimeUtc,
            NumberOfGuests = booking.NumberOfGuests,
            Note = booking.Note,
            HasSession = await db.Sessions.AnyAsync(session => session.BookingId == booking.BookingId, ct),
            Status = booking.Status,
            EstimatedAmount = booking.EstimatedAmount,
            RequiresApproval = booking.RequiresApproval,
            ApprovedAtUtc = booking.ApprovedAtUtc,
            HoldExpiresAtUtc = booking.HoldExpiresAtUtc,
            CancellationReason = booking.CancellationReason,
            NoShowAtUtc = booking.NoShowAtUtc,
            Source = booking.Source,
            StatusText = GetBookingStatusText(booking.Status),
            DepositStatusText = deposit is null ? null : GetDepositStatusText(deposit.Status),
            DepositPaymentInstruction = instruction,
            DepositRefundSummary = refundSummary,
            Deposit = deposit is null ? null : new BookingDepositDto
            {
                BookingDepositId = deposit.BookingDepositId,
                RequiredAmount = deposit.RequiredAmount,
                PaidAmount = deposit.PaidAmount,
                AppliedAmount = deposit.AppliedAmount,
                RefundedAmount = deposit.RefundedAmount,
                ForfeitedAmount = deposit.ForfeitedAmount,
                Status = deposit.Status,
                DueAtUtc = deposit.DueAtUtc,
                PaidAtUtc = deposit.PaidAtUtc
            }
        };
    }

    private async Task<DepositPaymentInstructionDto?> BuildDepositPaymentInstructionAsync(
        EntityBooking booking,
        string phoneNumber,
        PoolHub.Core.Entities.BookingDeposit deposit,
        CancellationToken ct)
    {
        if (booking.Status != BookingStatuses.PendingDeposit ||
            deposit.Status is not (BookingDepositStatuses.Pending or BookingDepositStatuses.PendingVerification))
        {
            return null;
        }

        var settings = await GetBankTransferQrConfigAsync(required: false, ct);
        if (settings is null) return null;

        var transferContent = FormatBookingDepositTransferContent(booking.BookingCode, phoneNumber);
        string? vietQrUrl = null;

        var clientId = config?["PayOSSettings:ClientId"];
        var apiKey = config?["PayOSSettings:ApiKey"];
        var checksumKey = config?["PayOSSettings:ChecksumKey"];
        var baseUrl = config?["EmailSettings:FrontendBaseUrl"] ?? "http://localhost:3000";

        if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(checksumKey) && httpClientFactory != null && deposit.BookingDepositId > 0)
        {
            try
            {
                long orderCode = 100000000 + deposit.BookingDepositId;
                long amount = (long)deposit.RequiredAmount;
                if (amount <= 0) amount = (long)MinimumDepositAmount;
                string cancelUrl = $"{baseUrl}/booking";
                string returnUrl = $"{baseUrl}/booking";
                string description = $"DEP {deposit.BookingDepositId}";
                string? payosQrString = null;
                long activePayOsOrderCode = orderCode;

                var client = httpClientFactory.CreateClient();

                string rawData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                var payosBody = new
                {
                    orderCode = orderCode,
                    amount = amount,
                    description = description,
                    cancelUrl = cancelUrl,
                    returnUrl = returnUrl,
                    signature = signature
                };

                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
                req.Headers.Add("x-client-id", clientId);
                req.Headers.Add("x-api-key", apiKey);
                req.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payosBody), System.Text.Encoding.UTF8, "application/json");

                var res = await client.SendAsync(req, ct);
                var resStr = await res.Content.ReadAsStringAsync(ct);

                if (res.IsSuccessStatusCode && !resStr.Contains("\"code\":\"233\""))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(resStr);
                    if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (dataEl.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            payosQrString = qrEl.GetString();
                        }
                    }
                }

                if (resStr.Contains("\"code\":\"233\"") || resStr.Contains("\"233\"") || resStr.Contains("tồn tại") || resStr.Contains("exists"))
                {
                    try
                    {
                        var checkReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{orderCode}");
                        checkReq.Headers.Add("x-client-id", clientId);
                        checkReq.Headers.Add("x-api-key", apiKey);
                        var checkRes = await client.SendAsync(checkReq, ct);
                        bool needNewOrderCode = true;

                        if (checkRes.IsSuccessStatusCode)
                        {
                            var checkStr = await checkRes.Content.ReadAsStringAsync(ct);
                            using var checkDoc = System.Text.Json.JsonDocument.Parse(checkStr);
                            if (checkDoc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object)
                            {
                                var st = data.TryGetProperty("status", out var stProp) ? stProp.GetString() : null;
                                decimal existingAmt = 0m;
                                if (data.TryGetProperty("amount", out var amProp) && amProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                                {
                                    amProp.TryGetDecimal(out existingAmt);
                                }

                                if (string.Equals(st, "PENDING", StringComparison.OrdinalIgnoreCase) && existingAmt == amount)
                                {
                                    if (data.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        payosQrString = qrEl.GetString();
                                    }
                                    if (!string.IsNullOrWhiteSpace(payosQrString))
                                    {
                                        needNewOrderCode = false;
                                    }
                                }
                            }
                        }

                        if (needNewOrderCode)
                        {
                            long newOrderCode = long.Parse($"{DateTime.UtcNow:yyMMddHHmmss}{Random.Shared.Next(10, 99)}");
                            activePayOsOrderCode = newOrderCode;
                            string newRawData = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={newOrderCode}&returnUrl={returnUrl}";
                            using var newHmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(checksumKey));
                            var newHash = newHmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(newRawData));
                            var newSignature = BitConverter.ToString(newHash).Replace("-", "").ToLower();

                            var newBody = new
                            {
                                orderCode = newOrderCode,
                                amount = amount,
                                description = description,
                                cancelUrl = cancelUrl,
                                returnUrl = returnUrl,
                                signature = newSignature
                            };

                            var retryReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests");
                            retryReq.Headers.Add("x-client-id", clientId);
                            retryReq.Headers.Add("x-api-key", apiKey);
                            retryReq.Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(newBody), System.Text.Encoding.UTF8, "application/json");
                            var retryRes = await client.SendAsync(retryReq, ct);
                            var retryStr = await retryRes.Content.ReadAsStringAsync(ct);

                            if (retryRes.IsSuccessStatusCode && !retryStr.Contains("\"code\":\"233\""))
                            {
                                using var retryDoc = System.Text.Json.JsonDocument.Parse(retryStr);
                                if (retryDoc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    if (dataEl.TryGetProperty("qrCode", out var qrEl) && qrEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        payosQrString = qrEl.GetString();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Error checking or retrying existing deposit order code #{OrderCode}", orderCode);
                    }
                }

                if (!string.IsNullOrWhiteSpace(payosQrString))
                {
                    vietQrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={Uri.EscapeDataString(payosQrString)}";
                    var currentNote = System.Text.RegularExpressions.Regex.Replace(booking.Note ?? "", @"PayOS_OrderCode:\d+\s*\|?\s*", "").Trim(' ', '|');
                    booking.Note = string.IsNullOrWhiteSpace(currentNote) ? $"PayOS_OrderCode:{activePayOsOrderCode}" : $"{currentNote} | PayOS_OrderCode:{activePayOsOrderCode}";
                    try { await db.SaveChangesAsync(ct); } catch { }
                }
                else
                {
                    // To help debug, throw with the actual PayOS response
                    var payosError = "Không có phản hồi từ PayOS";
                    if (resStr != null)
                    {
                        using var errDoc = System.Text.Json.JsonDocument.Parse(resStr);
                        if (errDoc.RootElement.TryGetProperty("desc", out var descEl) && descEl.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            payosError = descEl.GetString() ?? payosError;
                        }
                    }
                    logger.LogWarning("PayOS did not return a QR for deposit #{DepositId}. Falling back to bank transfer QR. Error: {PayOsError}", deposit.BookingDepositId, payosError);
                    if (settings.CanBuildDynamicQr)
                    {
                        vietQrUrl = BankTransferQrHelper.BuildVietQrUrl(settings, deposit.RequiredAmount, transferContent);
                    }
                }
            }
            catch (BusinessRuleException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error creating PayOS payment request for deposit #{DepositId}", deposit.BookingDepositId);
                if (settings.CanBuildDynamicQr)
                {
                    vietQrUrl = BankTransferQrHelper.BuildVietQrUrl(settings, deposit.RequiredAmount, transferContent);
                }
            }
        }
        else if (settings.CanBuildDynamicQr)
        {
            vietQrUrl = BankTransferQrHelper.BuildVietQrUrl(settings, deposit.RequiredAmount, transferContent);
        }

        return new DepositPaymentInstructionDto
        {
            PaymentMethodCode = settings.PaymentMethodCode,
            PaymentMethodName = settings.PaymentMethodName,
            BankName = BankTransferQrHelper.ResolveBankName(settings.BankCode, settings.BankName),
            BankCode = settings.BankCode,
            BankAccountNumber = settings.AccountNumber,
            BankAccountName = settings.AccountName,
            QrImageUrl = settings.QrImageUrl,
            VietQrUrl = vietQrUrl,
            Amount = deposit.RequiredAmount,
            TransferContent = transferContent,
            ExpiresAtUtc = booking.HoldExpiresAtUtc ?? deposit.DueAtUtc
        };
    }

    private async Task<BankTransferQrConfig?> GetBankTransferQrConfigAsync(bool required, CancellationToken ct)
    {
        var methods = await db.PaymentMethods.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var config = methods
            .Select(BankTransferQrHelper.Parse)
            .FirstOrDefault(x => x is not null && (x.CanBuildDynamicQr || !string.IsNullOrWhiteSpace(x.QrImageUrl)));
        if (required && config?.CanBuildDynamicQr != true)
        {
            throw new BusinessRuleException("Active bank transfer payment method is not configured for booking deposit QR.");
        }

        return config;
    }

    private static string FormatBookingDepositTransferContent(string bookingCode, string phoneNumber) =>
        $"POOLHUB {bookingCode} {phoneNumber}".Trim();

    private static string GetBookingStatusText(int status) => status switch
    {
        BookingStatuses.Pending => "Chờ xác nhận",
        BookingStatuses.Confirmed => "Đã xác nhận",
        BookingStatuses.Cancelled => "Đã hủy",
        BookingStatuses.Completed => "Đã hoàn thành",
        BookingStatuses.NoShow => "Không đến",
        BookingStatuses.PendingDeposit => "Chờ thanh toán cọc",
        BookingStatuses.PendingApproval => "Chờ quản lý duyệt",
        BookingStatuses.Expired => "Hết hạn",
        _ => $"Trạng thái {status}"
    };

    private static string GetDepositStatusText(int status) => status switch
    {
        BookingDepositStatuses.NotRequired => "Không yêu cầu cọc",
        BookingDepositStatuses.Pending => "Chờ thanh toán cọc",
        BookingDepositStatuses.PendingVerification => "Chờ xác minh cọc",
        BookingDepositStatuses.Paid => "Đã thanh toán cọc",
        BookingDepositStatuses.AppliedToInvoice => "Đã áp dụng vào hóa đơn",
        BookingDepositStatuses.Refunded => "Đã hoàn cọc",
        BookingDepositStatuses.PartiallyRefunded => "Đã hoàn một phần",
        BookingDepositStatuses.Forfeited => "Đã mất cọc",
        BookingDepositStatuses.Expired => "Hết hạn",
        _ => $"Trạng thái cọc {status}"
    };

    private async Task<List<long>> GetBookingTableIdsAsync(EntityBooking booking, CancellationToken ct)
    {
        var ids = await db.BookingTables.AsNoTracking()
            .Where(x => x.BookingId == booking.BookingId)
            .Select(x => x.TableId)
            .ToListAsync(ct);
        if (ids.Count == 0 && booking.TableId.HasValue) ids.Add(booking.TableId.Value);
        return ids.Distinct().ToList();
    }

    private async Task ReplaceBookingTablesAsync(long bookingId, List<long> tableIds, CancellationToken ct)
    {
        var existing = await db.BookingTables.Where(x => x.BookingId == bookingId).ToListAsync(ct);
        db.BookingTables.RemoveRange(existing);
        db.BookingTables.AddRange(tableIds.Select(tableId => new PoolHub.Core.Entities.BookingTable { BookingId = bookingId, TableId = tableId }));
    }

    private async Task ApplyAutomaticBookingStatusesAsync(DateTime nowUtc, CancellationToken ct)
    {
        await ExpirePendingDepositsAsync(nowUtc, ct);
        var candidates = await db.Bookings
            .Where(booking =>
                (booking.Status == BookingStatuses.Pending && booking.StartTimeUtc <= nowUtc) ||
                (booking.Status == BookingStatuses.Confirmed && booking.EndTimeUtc <= nowUtc))
            .Where(booking => !db.Sessions.Any(session => session.BookingId == booking.BookingId))
            .ToListAsync(ct);
        var changed = false;
        foreach (var booking in candidates)
        {
            if (booking.Status == BookingStatuses.Pending)
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.CancelledAtUtc = nowUtc;
                booking.Note = AppendAutomaticNote(booking.Note, "Auto-cancelled because booking was not confirmed before start time.");
            }
            else
            {
                booking.Status = BookingStatuses.NoShow;
                booking.NoShowAtUtc = nowUtc;
                booking.Note = AppendAutomaticNote(booking.Note, "Auto no-show because confirmed booking ended without starting session.");
            }
            booking.UpdatedAtUtc = nowUtc;
            changed = true;
        }

        if (changed) await db.SaveChangesAsync(ct);
    }

    private async Task<bool> ApplyAutomaticBookingStatusAsync(EntityBooking booking, DateTime nowUtc, CancellationToken ct)
    {
        if (booking.Status == BookingStatuses.Pending && booking.StartTimeUtc <= nowUtc)
        {
            booking.Status = BookingStatuses.Cancelled;
            booking.CancelledAtUtc = nowUtc;
            booking.UpdatedAtUtc = nowUtc;
            return true;
        }

        if (booking.Status == BookingStatuses.PendingDeposit && booking.HoldExpiresAtUtc <= nowUtc)
        {
            booking.Status = BookingStatuses.Expired;
            booking.UpdatedAtUtc = nowUtc;
            var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
            if (deposit is not null && deposit.Status is BookingDepositStatuses.Pending or BookingDepositStatuses.PendingVerification)
                deposit.Status = BookingDepositStatuses.Expired;
            return true;
        }

        if (booking.Status == BookingStatuses.Confirmed && booking.EndTimeUtc <= nowUtc &&
            !await db.Sessions.AnyAsync(session => session.BookingId == booking.BookingId, ct))
        {
            booking.Status = BookingStatuses.NoShow;
            booking.NoShowAtUtc = nowUtc;
            booking.UpdatedAtUtc = nowUtc;
            return true;
        }

        return false;
    }

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        return db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true
            ? null
            : await db.Database.BeginTransactionAsync(ct);
    }

    private static List<long> NormalizeTableIds(IEnumerable<long>? tableIds, long? tableId)
    {
        var result = (tableIds ?? []).Where(x => x > 0).Distinct().ToList();
        if (tableId.HasValue && tableId.Value > 0 && !result.Contains(tableId.Value)) result.Insert(0, tableId.Value);
        return result;
    }

    private static bool IsAlignedToThirtyMinutes(DateTime value) =>
        value.Minute % 30 == 0 && value.Second == 0 && value.Millisecond == 0;

    private static bool RuleMatchesLocalTime(int ruleDayType, TimeSpan startTime, TimeSpan endTime, int localDayType, int previousDayType, TimeSpan localTime)
    {
        if (startTime < endTime)
        {
            return ruleDayType == localDayType && startTime <= localTime && localTime < endTime;
        }

        if (startTime > endTime)
        {
            return (ruleDayType == localDayType && localTime >= startTime) ||
                   (ruleDayType == previousDayType && localTime < endTime);
        }

        return ruleDayType == localDayType;
    }

    private async Task<int> GetDayTypeAsync(DateTime date, CancellationToken ct)
    {
        var specialDate = await db.PricingSpecialDates
            .Where(x => x.Date.Date == date.Date)
            .FirstOrDefaultAsync(ct);
        
        if (specialDate != null)
        {
            return specialDate.DayType;
        }
        
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? 2 : 1;
    }


    private static void ValidateBookingPeriod(DateTime startTimeUtc, DateTime endTimeUtc)
    {
        if (startTimeUtc.Kind != DateTimeKind.Utc || endTimeUtc.Kind != DateTimeKind.Utc)
            throw new ValidationException("Booking times must use UTC.");
        if (endTimeUtc <= startTimeUtc)
            throw new ValidationException("End time must be after start time.");
    }

    private static void ValidateCalendarRequest(BookingCalendarRequest request)
    {
        if (request.From == default)
            throw new ValidationException("'from' is required.");
        if (request.To == default)
            throw new ValidationException("'to' is required.");
        if (request.From.Kind != DateTimeKind.Utc || request.To.Kind != DateTimeKind.Utc)
            throw new ValidationException("'from' and 'to' must be ISO-8601 UTC values.");
        if (request.From >= request.To)
            throw new ValidationException("'from' must be earlier than 'to'.");
    }

    private static string AppendAutomaticNote(string? note, string reason) =>
        string.IsNullOrWhiteSpace(note) ? reason : $"{note.Trim()} | {reason}";

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private async Task<(string? Email, string CustomerName, string PhoneNumber, string TableName)> GetEmailContextAsync(EntityBooking booking, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.CustomerId == booking.CustomerId)
            .Select(c => new { c.FullName, c.Email, c.PhoneNumber })
            .FirstOrDefaultAsync(ct);
        if (customer == null) return (null, "Khách hàng", "N/A", "N/A");

        var tableName = booking.TableId.HasValue
            ? await db.VenueTables.AsNoTracking()
                .Where(t => t.TableId == booking.TableId.Value)
                .Select(t => t.TableName)
                .FirstOrDefaultAsync(ct) ?? "N/A"
            : "Chưa chỉ định bàn";

        return (customer.Email, customer.FullName ?? "Khách hàng", customer.PhoneNumber ?? "N/A", tableName);
    }

    private async Task TrySendBookingConfirmedEmailAsync(EntityBooking booking, CancellationToken ct)
    {
        try
        {
            var (email, customerName, phoneNumber, tableName) = await GetEmailContextAsync(booking, ct);
            if (string.IsNullOrWhiteSpace(email)) return;

            var bookingCode = booking.BookingCode;
            var startUtc = booking.StartTimeUtc;
            var endUtc = booking.EndTimeUtc;
            var guests = booking.NumberOfGuests;
            var bookingId = booking.BookingId;

            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendBookingConfirmedAsync(email, customerName, phoneNumber, bookingCode, tableName,
                        startUtc, endUtc, guests, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send booking confirmed email for booking {BookingId}", bookingId);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to prepare booking confirmed email for booking {BookingId}", booking.BookingId);
        }
    }

    private async Task TrySendBookingCancelledEmailAsync(EntityBooking booking, string reason, CancellationToken ct)
    {
        try
        {
            var (email, customerName, phoneNumber, tableName) = await GetEmailContextAsync(booking, ct);
            if (string.IsNullOrWhiteSpace(email)) return;

            var bookingCode = booking.BookingCode;
            var startUtc = booking.StartTimeUtc;
            var endUtc = booking.EndTimeUtc;
            var guests = booking.NumberOfGuests;
            var bookingId = booking.BookingId;

            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendBookingCancelledAsync(email, customerName, phoneNumber, bookingCode, tableName,
                        startUtc, endUtc, guests, reason, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send booking cancelled email for booking {BookingId}", bookingId);
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to prepare booking cancelled email for booking {BookingId}", booking.BookingId);
        }
    }

    private async Task CreateBookingConfirmedNotificationAsync(EntityBooking booking, CancellationToken ct)
    {
        try
        {
            var customerName = await db.Customers.Where(c => c.CustomerId == booking.CustomerId).Select(c => c.FullName).FirstOrDefaultAsync(ct) ?? "Khách hàng";
            var tableName = booking.TableId.HasValue
                ? await db.VenueTables.Where(t => t.TableId == booking.TableId.Value).Select(t => t.TableName).FirstOrDefaultAsync(ct) ?? "Bàn"
                : "Bàn";
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(BusinessTime.NormalizeUtc(booking.StartTimeUtc), BusinessTime.TimeZone);
            var startTimeStr = localStart.ToString("HH:mm dd/MM/yyyy");

            db.Notifications.Add(new PoolHub.Core.Entities.Notification
            {
                UserId = null,
                CustomerId = booking.CustomerId,
                Title = "Đặt bàn mới",
                Message = $"[Đặt bàn mới] Khách hàng {customerName} vừa đặt bàn {tableName} lúc {startTimeStr}. Mã BK: {booking.BookingCode}",
                NotificationType = "BOOKING",
                IsRead = false,
                CreatedAtUtc = _clock.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create confirmed booking notification for booking {BookingId}", booking.BookingId);
        }
    }

    private async Task CheckAndConfirmDepositFromPayOsAsync(EntityBooking booking, CancellationToken ct)
    {
        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x => x.BookingId == booking.BookingId, ct);
        if (deposit == null || (deposit.Status != BookingDepositStatuses.Pending && deposit.Status != BookingDepositStatuses.PendingVerification))
            return;

        var clientId = config?["PayOSSettings:ClientId"];
        var apiKey = config?["PayOSSettings:ApiKey"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(apiKey) || httpClientFactory == null)
            return;

        var orderCodesToCheck = new HashSet<long> { 100000000 + deposit.BookingDepositId };
        if (!string.IsNullOrEmpty(booking.Note))
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(booking.Note, @"PayOS_OrderCode:(\d+)");
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                if (long.TryParse(m.Groups[1].Value, out var c)) orderCodesToCheck.Add(c);
            }
        }

        var client = httpClientFactory.CreateClient();
        foreach (var checkCode in orderCodesToCheck)
        {
            try
            {
                var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{checkCode}?_t={DateTime.UtcNow.Ticks}");
                req.Headers.Add("x-client-id", clientId);
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                req.Headers.Add("Pragma", "no-cache");
                var res = await client.SendAsync(req, ct);
                if (res.IsSuccessStatusCode)
                {
                    var resStr = await res.Content.ReadAsStringAsync(ct);
                    using var doc = System.Text.Json.JsonDocument.Parse(resStr);
                    if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
                        decimal amountPaid = 0m;
                        if (data.TryGetProperty("amountPaid", out var ap) && ap.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            ap.TryGetDecimal(out amountPaid);
                        }
                        decimal amount = 0m;
                        if (data.TryGetProperty("amount", out var am) && am.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            am.TryGetDecimal(out amount);
                        }

                        if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase) || (amountPaid > 0 && amountPaid >= deposit.RequiredAmount))
                        {
                            var payAmount = amountPaid > 0 ? amountPaid : (amount > 0 ? amount : deposit.RequiredAmount);
                            await ConfirmDepositAsync(booking.BookingId, new ConfirmDepositRequest
                            {
                                PaidAmount = payAmount,
                                TransactionCode = $"PAYOS_{checkCode}"
                            }, 1, ct);
                            await db.Entry(booking).ReloadAsync(ct);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error checking PayOS deposit status for booking #{BookingId}, checkCode #{CheckCode}", booking.BookingId, checkCode);
            }
        }
    }
}

internal sealed class NoOpPosNotificationService : IPosNotificationService
{
    public Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default) => Task.CompletedTask;
    public Task NotifyRefreshPosAsync(CancellationToken ct = default) => Task.CompletedTask;
}
