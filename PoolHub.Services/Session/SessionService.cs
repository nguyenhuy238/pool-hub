using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;
using System.Data;
using System.Text.Json;
using EntitySession = PoolHub.Core.Entities.Session;
using EntityInvoice = PoolHub.Core.Entities.Invoice;
using EntityBooking = PoolHub.Core.Entities.Booking;
using Microsoft.EntityFrameworkCore.Storage;

namespace PoolHub.Services.Session;

public class SessionService(PoolHubDbContext db, IPosNotificationService posNotificationService, IConfiguration? config = null, IClock? clock = null) : ISessionService
{
    private const int DefaultEarlyCheckInMinutes = 15;
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public SessionService(PoolHubDbContext db) : this(db, new NoOpPosNotificationService(), null, null)
    {
    }

    public async Task<PagedResult<SessionDto>> GetSessionsAsync(SessionQueryRequest request, CancellationToken ct)
    {
        var query = db.Sessions.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.Date.HasValue)
        {
            var (fromUtc, toUtc) = BusinessTime.LocalDateRangeToUtc(request.Date.Value);
            query = query.Where(x => x.StartedAtUtc >= fromUtc && x.StartedAtUtc < toUtc);
        }

        if (request.TableId.HasValue)
        {
            query = query.Where(x => db.SessionTableAssignments.Any(sta => sta.SessionId == x.SessionId && sta.TableId == request.TableId.Value));
        }

        var total = await query.CountAsync(ct);
        var rawItems = await query
            .OrderByDescending(x => x.SessionId)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.SessionId,
                x.SessionCode,
                x.Status,
                x.StartedAtUtc,
                x.EndedAtUtc,
                TableName = db.SessionTableAssignments
                    .Where(sta => sta.SessionId == x.SessionId)
                    .OrderByDescending(sta => sta.SessionTableAssignmentId)
                    .Join(db.VenueTables, sta => sta.TableId, vt => vt.TableId, (sta, vt) => vt.TableName)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        var items = rawItems.Select(x => new SessionDto
        {
            SessionId = x.SessionId,
            SessionCode = x.SessionCode,
            Status = x.Status,
            StartedAtUtc = x.StartedAtUtc,
            EndedAtUtc = x.EndedAtUtc,
            TableName = !string.IsNullOrEmpty(x.TableName) ? x.TableName : $"BĂ n #{x.SessionId}",
            DurationMinutes = x.Status == 1
                ? (int)Math.Max(0, Math.Ceiling((now - x.StartedAtUtc).TotalMinutes))
                : (x.EndedAtUtc.HasValue ? (int)Math.Max(0, Math.Ceiling((x.EndedAtUtc.Value - x.StartedAtUtc).TotalMinutes)) : 0)
        }).ToList();

        return new PagedResult<SessionDto> { Items = items, PageNumber = request.PageNumber, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task<List<ActiveSessionResponse>> GetActiveSessionsAsync(long? floorId, long? zoneId, long? tableId, CancellationToken ct)
    {
        var query = from session in db.Sessions.AsNoTracking()
                    join assignment in db.SessionTableAssignments.AsNoTracking() on session.SessionId equals assignment.SessionId
                    join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                    join zone in db.Zones.AsNoTracking() on table.ZoneId equals zone.ZoneId
                    where session.Status == 1 && assignment.EndedAtUtc == null
                    select new { Session = session, Assignment = assignment, Table = table, Zone = zone };

        if (floorId.HasValue)
        {
            query = query.Where(x => x.Zone.FloorId == floorId.Value);
        }

        if (zoneId.HasValue)
        {
            query = query.Where(x => x.Table.ZoneId == zoneId.Value);
        }

        if (tableId.HasValue)
        {
            query = query.Where(x => x.Table.TableId == tableId.Value);
        }

        var activeRows = await query
            .OrderByDescending(x => x.Session.StartedAtUtc)
            .Select(x => new ActiveSessionResponse
            {
                SessionId = x.Session.SessionId,
                SessionCode = x.Session.SessionCode,
                Status = x.Session.Status,
                StartedAtUtc = x.Session.StartedAtUtc,
                CustomerId = x.Session.CustomerId,
                BookingId = x.Session.BookingId,
                CurrentTable = new ActiveSessionTableDto
                {
                    AssignmentId = x.Assignment.SessionTableAssignmentId,
                    TableId = x.Table.TableId,
                    TableCode = x.Table.TableCode,
                    TableName = x.Table.TableName,
                    ZoneId = x.Table.ZoneId,
                    FloorId = x.Zone.FloorId,
                    StartedAtUtc = x.Assignment.StartedAtUtc,
                    HourlyRateSnapshot = x.Assignment.HourlyRateSnapshot
                }
            })
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        var sessionIds = activeRows.Select(x => x.SessionId).ToList();
        var customerIds = activeRows
            .Where(x => x.CustomerId.HasValue)
            .Select(x => x.CustomerId!.Value)
            .Distinct()
            .ToList();
        var assignments = await db.SessionTableAssignments.AsNoTracking()
            .Where(x => sessionIds.Contains(x.SessionId))
            .Select(x => new
            {
                x.SessionId,
                x.StartedAtUtc,
                x.EndedAtUtc,
                x.DurationMinutes
            })
            .ToListAsync(ct);

        var durationBySession = assignments
            .GroupBy(x => x.SessionId)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(assignment => GetDurationMinutes(assignment.StartedAtUtc, assignment.EndedAtUtc ?? now, assignment.DurationMinutes)));
        var customerNames = await db.Customers.AsNoTracking()
            .Where(x => customerIds.Contains(x.CustomerId))
            .Select(x => new { x.CustomerId, x.FullName })
            .ToDictionaryAsync(x => x.CustomerId, x => x.FullName, ct);

        foreach (var row in activeRows)
        {
            row.DurationMinutes = durationBySession.GetValueOrDefault(row.SessionId);
            row.CustomerName = row.CustomerId.HasValue && customerNames.TryGetValue(row.CustomerId.Value, out var customerName)
                ? customerName
                : "KhĂ¡ch vĂ£ng lai";
        }

        return activeRows;
    }

    public async Task<SessionDetailDto> GetActiveSessionByTableAsync(long tableId, CancellationToken ct)
    {
        var tableExists = await db.VenueTables.AsNoTracking().AnyAsync(x => x.TableId == tableId, ct);
        if (!tableExists)
        {
            throw new NotFoundException("Table not found.");
        }

        var sessionId = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                               join session in db.Sessions.AsNoTracking() on assignment.SessionId equals session.SessionId
                               where assignment.TableId == tableId && assignment.EndedAtUtc == null && session.Status == 1
                               orderby assignment.StartedAtUtc descending
                               select session.SessionId)
            .FirstOrDefaultAsync(ct);

        if (sessionId == 0)
        {
            throw new NotFoundException("Active session not found for this table.");
        }

        return await GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<SessionDetailDto> GetSessionByIdAsync(long id, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([id], ct) ?? throw new NotFoundException("Session not found.");
        
        var assignments = await (from sta in db.SessionTableAssignments
                                 join t in db.VenueTables on sta.TableId equals t.TableId
                                 where sta.SessionId == id
                                 select new SessionTableAssignmentDto
                                 {
                                     SessionTableAssignmentId = sta.SessionTableAssignmentId,
                                     SessionId = sta.SessionId,
                                     TableId = sta.TableId,
                                     TableName = t.TableName,
                                     TableCode = t.TableCode,
                                     PricingPlanRuleId = sta.PricingPlanRuleId,
                                     StartedAtUtc = sta.StartedAtUtc,
                                     EndedAtUtc = sta.EndedAtUtc,
                                     DurationMinutes = sta.DurationMinutes,
                                     HourlyRateSnapshot = sta.HourlyRateSnapshot,
                                     Amount = sta.Amount,
                                     AssignedByUserId = sta.AssignedByUserId,
                                     Note = sta.Note
                                 })
                                 .ToListAsync(ct);

        return new SessionDetailDto
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            CustomerId = session.CustomerId,
            BookingId = session.BookingId,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            OpenedByUserId = session.OpenedByUserId,
            ClosedByUserId = session.ClosedByUserId,
            Note = session.Note,
            Assignments = assignments
        };
    }

    public async Task<SessionSummaryResponse> GetSummaryAsync(long sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        var now = _clock.UtcNow;
        var timeCharge = await CalculateSessionTimeChargeAsync(sessionId, now, false, ct);
        var assignmentDtos = timeCharge.Lines;

        var currentAssignment = assignmentDtos.LastOrDefault(x => x.IsCurrent);
        var orders = await GetSessionSummaryOrdersAsync(sessionId, ct);
        var orderSubtotal = orders.Sum(x => x.SubtotalAmount);
        var timeSubtotal = assignmentDtos.Sum(x => x.Amount);
        var existingInvoice = await db.Invoices
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.InvoiceId)
            .FirstOrDefaultAsync(ct);
        if (existingInvoice != null && existingInvoice.Status != 3 && existingInvoice.PaymentStatus != InvoicePaymentStatuses.Paid && session.BookingId.HasValue)
        {
            await ApplyBookingDepositToInvoiceAsync(session, existingInvoice, ct);
            await db.SaveChangesAsync(ct);
        }
        var subtotal = timeSubtotal + orderSubtotal;
        var discountAmount = existingInvoice?.DiscountAmount ?? 0;
        
        var depositAmount = 0m;
        if (session.BookingId.HasValue)
        {
            depositAmount = await db.BookingDeposits.AsNoTracking()
                .Where(x => x.BookingId == session.BookingId.Value && x.Status == PoolHub.Shared.Constants.BookingDepositStatuses.Paid)
                .SumAsync(x => x.PaidAmount, ct);
        }

        return new SessionSummaryResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            PreviewEndedAtUtc = session.EndedAtUtc ?? now,
            CurrentDurationMinutes = timeCharge.ActualDurationMinutes,
            ActualDurationMinutes = timeCharge.ActualDurationMinutes,
            BillableDurationMinutes = timeCharge.BillableDurationMinutes,
            TimeSubtotalAmount = timeSubtotal,
            OrderSubtotalAmount = orderSubtotal,
            ProductSubtotalAmount = orderSubtotal,
            SubtotalAmount = subtotal,
            DiscountAmount = discountAmount,
            GrandTotalAmount = existingInvoice?.GrandTotalAmount ?? Math.Max(0, subtotal - discountAmount),
            InvoiceId = existingInvoice?.InvoiceId,
            InvoiceCode = existingInvoice?.InvoiceCode,
            InvoiceStatus = existingInvoice?.Status,
            DepositAmount = depositAmount,
            CurrentTable = currentAssignment is null
                ? null
                : new SessionSummaryTableDto
                {
                    TableId = currentAssignment.TableId,
                    TableCode = currentAssignment.TableCode,
                    TableName = currentAssignment.TableName
                },
            TimeCharge = timeCharge,
            Assignments = assignmentDtos,
            Orders = orders
        };
    }

    public async Task<SessionTimeChargesResponse> GetTimeChargesAsync(long sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
            ?? throw new NotFoundException("Session not found.");
        var now = _clock.UtcNow;
        var charge = await CalculateSessionTimeChargeAsync(sessionId, now, false, ct);
        var items = charge.Lines.Select(x => new SessionTimeChargeItemDto
        {
            AssignmentId = x.SessionTableAssignmentId,
            TableId = x.TableId,
            TableCode = x.TableCode,
            TableName = x.TableName,
            PricingPlanRuleId = x.PricingPlanRuleId,
            StartedAtUtc = x.StartedAtUtc,
            EndedAtUtc = x.EndedAtUtc,
            DurationMinutes = x.DurationMinutes,
            ActualDurationMinutes = x.ActualDurationMinutes,
            BillableMinutes = x.BillableDurationMinutes,
            HourlyRateSnapshot = x.HourlyRateSnapshot,
            HourlyRate = x.HourlyRate,
            MinimumMinutes = x.MinimumMinutes,
            BillingBlockMinutes = x.BillingBlockMinutes,
            PricingPlanName = x.PricingPlanName,
            Amount = x.Amount,
            IsBillable = x.IsBillable,
            Note = x.Note
        }).ToList();

        return new SessionTimeChargesResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            ServerNowUtc = now,
            ActualDurationMinutes = charge.ActualDurationMinutes,
            BillableDurationMinutes = charge.BillableDurationMinutes,
            MinimumMinutes = charge.MinimumMinutes,
            BillingBlockMinutes = charge.BillingBlockMinutes,
            TotalAmount = items.Sum(x => x.Amount),
            Note = charge.Note,
            Items = items
        };
    }

    public async Task<SessionDto> StartAsync(long userId, StartSessionRequest request, CancellationToken ct)
    {
        var booking = request.BookingId.HasValue
            ? await db.Bookings.FindAsync([request.BookingId.Value], ct) ?? throw new NotFoundException("Booking not found.")
            : null;

        if (booking is not null)
        {
            var hasSession = await db.Sessions.AnyAsync(x => x.BookingId == booking.BookingId, ct);
            if (hasSession)
            {
                throw new ConflictException("Booking already has a session.");
            }

            var nowUtc = _clock.UtcNow;
            var bookingStartUtc = NormalizeUtc(booking.StartTimeUtc);
            var bookingEndUtc = NormalizeUtc(booking.EndTimeUtc);
            if (booking.Status == BookingStatuses.Pending && bookingStartUtc <= nowUtc)
            {
                booking.Status = BookingStatuses.Cancelled;
                booking.CancelledAtUtc = nowUtc;
                booking.Note = AppendAutomaticBookingNote(booking.Note, "Auto-cancelled because booking was not confirmed before start time.");
                booking.UpdatedAtUtc = nowUtc;
                await db.SaveChangesAsync(ct);
                throw new ConflictException("Booking has expired and cannot start a session.");
            }

            if (booking.Status == BookingStatuses.Confirmed && bookingEndUtc <= nowUtc)
            {
                booking.Status = BookingStatuses.NoShow;
                booking.Note = AppendAutomaticBookingNote(booking.Note, "Auto no-show because confirmed booking ended without starting session.");
                booking.UpdatedAtUtc = nowUtc;
                await db.SaveChangesAsync(ct);
                throw new ConflictException("Booking has ended and cannot start a session.");
            }

            if (booking.Status != BookingStatuses.Confirmed)
            {
                throw new BusinessRuleException("Only confirmed bookings can start a session.");
            }

        }

        var tableId = request.TableId > 0
            ? request.TableId
            : booking?.TableId ?? throw new BusinessRuleException("A table is required to start a session.");
        var customerId = booking?.CustomerId ?? request.CustomerId;

        await EnsureCustomerCanStartSessionAsync(customerId, null, ct);
        await EnsureTableHasNoActiveSessionAsync(tableId, "Table already has an active session.", ct);

        var table = await db.VenueTables.FindAsync([tableId], ct) ?? throw new NotFoundException("Table not found.");
        if (!table.IsActive || table.OperationalStatus is TableOperationalStatuses.Maintenance or TableOperationalStatuses.Inactive)
        {
            throw new BusinessRuleException("Table is not available for a new session.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);
        var startedAtUtc = _clock.UtcNow;

        var session = new EntitySession
        {
            SessionCode = $"SS{startedAtUtc:yyyyMMddHHmmss}",
            BookingId = booking?.BookingId,
            CustomerId = customerId,
            OpenedByUserId = userId,
            StartedAtUtc = startedAtUtc,
            Status = 1
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        // Fetch pricing plan and active rule for the table at start time
        var rule = await FindActiveRuleAsync(table.TableTypeId, session.StartedAtUtc, ct)
            ?? throw BuildMissingPricingRuleException(table, session.StartedAtUtc, "thá»i Ä‘iá»ƒm má»Ÿ phiĂªn");
        var hourlyRate = rule.HourlyRate;

        var assignment = new SessionTableAssignment
        {
            SessionId = session.SessionId,
            TableId = tableId,
            StartedAtUtc = startedAtUtc,
            AssignedByUserId = userId,
            PricingPlanRuleId = rule?.PricingPlanRuleId,
            HourlyRateSnapshot = hourlyRate
        };
        db.SessionTableAssignments.Add(assignment);

        if (booking is not null)
        {
            booking.Status = BookingStatuses.InProgress;
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);

        await posNotificationService.NotifySessionStartedAsync((int)session.SessionId, (int)tableId, ct);
        if (booking is not null)
        {
            await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        }

        return await MapSessionDtoAsync(session, ct);
    }

    public async Task<SessionDto> StartFromBookingAsync(long bookingId, long? tableId, long userId, CancellationToken ct)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var booking = await db.Bookings.FindAsync([bookingId], ct) ?? throw new NotFoundException("Booking not found.");
        if (await db.Sessions.AnyAsync(x => x.BookingId == booking.BookingId, ct))
        {
            throw new ConflictException("Booking already has a session.");
        }

        var nowUtc = _clock.UtcNow;
        var bookingStartUtc = NormalizeUtc(booking.StartTimeUtc);
        var bookingEndUtc = NormalizeUtc(booking.EndTimeUtc);
        if (booking.Status == BookingStatuses.Pending && bookingStartUtc <= nowUtc)
        {
            booking.Status = BookingStatuses.Cancelled;
            booking.CancelledAtUtc = nowUtc;
            booking.Note = AppendAutomaticBookingNote(booking.Note, "Auto-cancelled because booking was not confirmed before start time.");
            booking.UpdatedAtUtc = nowUtc;
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            throw new ConflictException("Booking has expired and cannot start a session.");
        }

        if (booking.Status == BookingStatuses.Confirmed && bookingEndUtc <= nowUtc)
        {
            booking.Status = BookingStatuses.NoShow;
            booking.Note = AppendAutomaticBookingNote(booking.Note, "Auto no-show because confirmed booking ended without starting session.");
            booking.UpdatedAtUtc = nowUtc;
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            throw new ConflictException("Booking has ended and cannot start a session.");
        }

        if (booking.Status != BookingStatuses.Confirmed)
        {
            throw new BusinessRuleException("Only confirmed bookings can start a session.");
        }


        var tableIds = await GetBookingTableIdsAsync(booking, ct);
        if (tableId.HasValue && tableId.Value > 0 && !tableIds.Contains(tableId.Value))
        {
            tableIds.Add(tableId.Value);
        }

        tableIds = tableIds.Where(x => x > 0).Distinct().OrderBy(x => x).ToList();
        if (tableIds.Count == 0)
        {
            throw new BusinessRuleException("Booking has no table to start a session.");
        }

        var tables = await db.VenueTables.Where(x => tableIds.Contains(x.TableId)).ToListAsync(ct);
        var missing = tableIds.Except(tables.Select(x => x.TableId)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"Table not found: {string.Join(", ", missing)}.");
        }

        var unavailable = tables.FirstOrDefault(x => !x.IsActive || x.OperationalStatus is TableOperationalStatuses.Maintenance or TableOperationalStatuses.Inactive);
        if (unavailable is not null)
        {
            throw new BusinessRuleException($"Table is not available for a new session: {unavailable.TableName}.");
        }

        foreach (var id in tableIds)
        {
            await EnsureTableHasNoActiveSessionAsync(id, "Table already has an active session.", ct);
        }

        var rules = new Dictionary<long, PricingPlanRule>();
        foreach (var table in tables)
        {
            rules[table.TableId] = await FindActiveRuleAsync(table.TableTypeId, nowUtc, ct)
                ?? throw BuildMissingPricingRuleException(table, nowUtc, "thá»i Ä‘iá»ƒm má»Ÿ phiĂªn");
        }

        await EnsureCustomerCanStartSessionAsync(booking.CustomerId, booking.BookingId, ct);

        var session = new EntitySession
        {
            SessionCode = $"SS{nowUtc:yyyyMMddHHmmss}",
            BookingId = booking.BookingId,
            CustomerId = booking.CustomerId,
            OpenedByUserId = userId,
            StartedAtUtc = nowUtc,
            Status = 1
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        foreach (var table in tables.OrderBy(x => x.TableId))
        {
            var rule = rules[table.TableId];
            db.SessionTableAssignments.Add(new SessionTableAssignment
            {
                SessionId = session.SessionId,
                TableId = table.TableId,
                StartedAtUtc = nowUtc,
                AssignedByUserId = userId,
                PricingPlanRuleId = rule.PricingPlanRuleId,
                HourlyRateSnapshot = rule.HourlyRate
            });
        }

        booking.Status = BookingStatuses.InProgress;
        booking.UpdatedAtUtc = nowUtc;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "SessionStartedFromMultiTableBooking",
            EntityName = nameof(EntitySession),
            EntityId = session.SessionId,
            NewValues = JsonSerializer.Serialize(new { booking.BookingId, TableIds = tableIds }),
            Description = $"Session {session.SessionCode} started from booking {booking.BookingCode} with {tableIds.Count} table(s)."
        });

        try
        {
            await db.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("One or more tables already have an active session.", [ex.Message]);
        }

        foreach (var id in tableIds)
        {
            await posNotificationService.NotifyTableUpdateAsync((int)id, ct);
        }
        await posNotificationService.NotifySessionUpdateAsync((int)session.SessionId, ct);
        await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);

        return await MapSessionDtoAsync(session, ct);
    }

    private static string AppendAutomaticBookingNote(string? note, string reason) =>
        string.IsNullOrWhiteSpace(note) ? reason : $"{note.Trim()} | {reason}";

    private int GetEarlyCheckInMinutes()
    {
        var configured = config?.GetValue<int?>("BookingRules:EarlyCheckInMinutes") ?? DefaultEarlyCheckInMinutes;
        return Math.Clamp(configured, 0, 240);
    }

    private async Task EnsureCustomerCanStartSessionAsync(long? customerId, long? bookingId, CancellationToken ct)
    {
        if (bookingId.HasValue)
        {
            var bookingCustomer = await db.Bookings
                .Where(x => x.BookingId == bookingId.Value)
                .Select(x => x.CustomerId)
                .FirstOrDefaultAsync(ct);

            if (bookingCustomer == 0)
            {
                throw new NotFoundException("Booking not found.");
            }

            customerId ??= bookingCustomer;
        }

        if (!customerId.HasValue)
        {
            return;
        }

        var customer = await db.Customers
            .Where(x => x.CustomerId == customerId.Value)
            .Select(x => new { x.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Customer not found.");

        if (!customer.Status)
        {
            throw new BusinessRuleException("Customer is blocked or inactive.");
        }
    }

    private async Task EnsureTableHasNoActiveSessionAsync(long tableId, string message, CancellationToken ct)
    {
        var hasActiveSession = await db.SessionTableAssignments
            .AnyAsync(sta => sta.TableId == tableId && sta.EndedAtUtc == null &&
                db.Sessions.Any(s => s.SessionId == sta.SessionId && s.Status == 1), ct);
        if (hasActiveSession)
        {
            throw new ConflictException(message);
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct, IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        if (db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            return null;
        }

        return await db.Database.BeginTransactionAsync(isolationLevel, ct);
    }

    private async Task<List<long>> GetBookingTableIdsAsync(EntityBooking booking, CancellationToken ct)
    {
        var tableIds = await db.BookingTables.AsNoTracking()
            .Where(x => x.BookingId == booking.BookingId)
            .Select(x => x.TableId)
            .ToListAsync(ct);

        if (booking.TableId.HasValue)
        {
            tableIds.Add(booking.TableId.Value);
        }

        return tableIds.Distinct().OrderBy(x => x).ToList();
    }

    private async Task<SessionDto> MapSessionDtoAsync(EntitySession session, CancellationToken ct)
    {
        var assignments = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                                 join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                                 where assignment.SessionId == session.SessionId
                                 orderby assignment.StartedAtUtc, assignment.SessionTableAssignmentId
                                 select new SessionTableAssignmentDto
                                 {
                                     SessionTableAssignmentId = assignment.SessionTableAssignmentId,
                                     SessionId = assignment.SessionId,
                                     TableId = assignment.TableId,
                                     TableCode = table.TableCode,
                                     TableName = table.TableName,
                                     PricingPlanRuleId = assignment.PricingPlanRuleId,
                                     StartedAtUtc = assignment.StartedAtUtc,
                                     EndedAtUtc = assignment.EndedAtUtc,
                                     DurationMinutes = assignment.DurationMinutes,
                                     HourlyRateSnapshot = assignment.HourlyRateSnapshot,
                                     Amount = assignment.Amount,
                                     AssignedByUserId = assignment.AssignedByUserId,
                                     Note = assignment.Note
                                 })
            .ToListAsync(ct);

        var now = _clock.UtcNow;
        var displayTable = assignments.FirstOrDefault(x => x.EndedAtUtc == null) ?? assignments.FirstOrDefault();
        return new SessionDto
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            TableName = displayTable?.TableName ?? string.Empty,
            DurationMinutes = session.Status == 1
                ? GetDurationMinutes(session.StartedAtUtc, now)
                : session.EndedAtUtc.HasValue ? GetDurationMinutes(session.StartedAtUtc, session.EndedAtUtc.Value) : 0,
            Assignments = assignments
        };
    }

    private async Task<List<long>> CloseActiveAssignmentsAsync(EntitySession session, DateTime endedAtUtc, long? closedByUserId, CancellationToken ct)
    {
        if (endedAtUtc <= session.StartedAtUtc)
        {
            throw new ValidationException("EndedAtUtc must be after session start time.");
        }

        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == session.SessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);
        if (activeAssignments.Count == 0)
        {
            throw new ConflictException("Session has no active table assignment to close.");
        }

        foreach (var assignment in activeAssignments)
        {
            await CloseAssignmentAsync(assignment, endedAtUtc, null, ct);
        }

        session.Status = 2;
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = closedByUserId;

        if (session.BookingId.HasValue)
        {
            var booking = await db.Bookings.FindAsync([session.BookingId.Value], ct);
            if (booking is not null)
            {
                booking.Status = BookingStatuses.Completed;
            }
        }

        return activeAssignments.Select(x => x.TableId).Distinct().ToList();
    }

    private async Task<EntityInvoice> CreateOrGetSessionInvoiceAsync(EntitySession session, long? issuedByUserId, CancellationToken ct)
    {
        if (await db.SessionTableAssignments.AnyAsync(x => x.SessionId == session.SessionId && x.EndedAtUtc == null, ct))
        {
            throw new ConflictException("Cannot generate invoice while the session still has active table assignments.");
        }

        var existing = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == session.SessionId && x.Status != 3, ct);
        if (existing is not null)
        {
            return existing;
        }

        var issuedAtUtc = session.EndedAtUtc ?? _clock.UtcNow;
        var timeCharge = await CalculateSessionTimeChargeAsync(session.SessionId, issuedAtUtc, true, ct);
        var productSubtotal = await db.Orders
            .Where(x => x.SessionId == session.SessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);

        var invoice = new EntityInvoice
        {
            SessionId = session.SessionId,
            CustomerId = session.CustomerId,
            InvoiceCode = $"INV{issuedAtUtc:yyyyMMddHHmmss}",
            TimeSubtotalAmount = timeCharge.SubtotalAmount,
            ProductSubtotalAmount = productSubtotal,
            SubtotalAmount = timeCharge.SubtotalAmount + productSubtotal,
            DiscountAmount = 0,
            TaxAmount = 0,
            GrandTotalAmount = timeCharge.SubtotalAmount + productSubtotal,
            PaidAmount = 0,
            PaymentStatus = 1,
            Status = 1,
            IssuedByUserId = issuedByUserId,
            IssuedAtUtc = issuedAtUtc
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        foreach (var line in timeCharge.Lines)
        {
            db.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.InvoiceId,
                LineType = "TIME",
                ReferenceId = line.SessionTableAssignmentId,
                Description = $"Table time {line.TableName}",
                Quantity = (decimal)line.BillableDurationMinutes / 60m,
                UnitPrice = line.HourlyRate,
                LineTotalAmount = line.Amount
            });
        }

        var orderItems = await (from order in db.Orders
                                join item in db.OrderItems on order.OrderId equals item.OrderId
                                where order.SessionId == session.SessionId && order.Status != 3
                                select item)
            .ToListAsync(ct);
        foreach (var item in orderItems)
        {
            db.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = invoice.InvoiceId,
                LineType = "PRODUCT",
                ReferenceId = item.OrderItemId,
                Description = item.ProductNameSnapshot,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPriceSnapshot,
                LineTotalAmount = item.LineTotalAmount
            });
        }

        await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);
        return invoice;
    }

    private async Task CloseAssignmentAsync(SessionTableAssignment assignment, DateTime endedAtUtc, string? note, CancellationToken ct)
    {
        if (assignment.EndedAtUtc.HasValue)
        {
            throw new ConflictException("Assignment is already ended.");
        }
        if (endedAtUtc <= assignment.StartedAtUtc)
        {
            throw new ValidationException("EndedAtUtc must be after assignment start time.");
        }

        var table = await db.VenueTables.FindAsync([assignment.TableId], ct)
            ?? throw new NotFoundException($"Table {assignment.TableId} not found.");
        var pricing = await GetAssignmentPricingAsync(assignment, table, ct);
        var actualMinutes = GetDurationMinutes(assignment.StartedAtUtc, endedAtUtc);
        var isBillable = IsAssignmentBillable(assignment, actualMinutes);
        var billableMinutes = isBillable ? ApplyBillingRules(actualMinutes, pricing.MinimumMinutes, pricing.BillingBlockMinutes) : 0;
        var hourlyRate = assignment.HourlyRateSnapshot > 0 ? assignment.HourlyRateSnapshot : pricing.HourlyRate;

        assignment.EndedAtUtc = endedAtUtc;
        assignment.DurationMinutes = actualMinutes;
        assignment.HourlyRateSnapshot = hourlyRate;
        assignment.PricingPlanRuleId ??= pricing.PricingPlanRuleId;
        assignment.Amount = isBillable
            ? Math.Round(((decimal)billableMinutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero)
            : 0;
        assignment.Note = AppendNote(assignment.Note, note);
    }

    private static int ApplyBillingRules(int actualMinutes, int minimumMinutes, int billingBlockMinutes)
    {
        var billableMinutes = Math.Max(actualMinutes, minimumMinutes);
        if (billingBlockMinutes <= 0)
        {
            return billableMinutes;
        }

        var remainder = billableMinutes % billingBlockMinutes;
        return remainder == 0 ? billableMinutes : billableMinutes + billingBlockMinutes - remainder;
    }

    private static string? AppendNote(string? currentNote, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return currentNote;
        }

        return string.IsNullOrWhiteSpace(currentNote) ? note.Trim() : $"{currentNote.Trim()} | {note.Trim()}";
    }

    public async Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            return await MapSessionDtoAsync(session, ct);
        }

        var endedAtUtc = _clock.UtcNow;
        var closedTableIds = await CloseActiveAssignmentsAsync(session, endedAtUtc, closedByUserId, ct);
        await db.SaveChangesAsync(ct);

        foreach (var tableId in closedTableIds)
        {
            await posNotificationService.NotifyTableUpdateAsync((int)tableId, ct);
        }
        await posNotificationService.NotifySessionClosedAsync((int)session.SessionId, closedTableIds.FirstOrDefault() is long closedTableId ? (int)closedTableId : null, ct);

        return await MapSessionDtoAsync(session, ct);
    }

    public async Task<CloseSessionResponse> CloseWithSummaryAsync(long sessionId, long? closedByUserId, CloseSessionRequest request, CancellationToken ct)
    {
        request ??= new CloseSessionRequest();

        var now = _clock.UtcNow;
        var endedAtUtc = request.EndedAtUtc ?? now;
        if (endedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException("EndedAtUtc must use UTC.");
        }
        if (endedAtUtc > now.AddMinutes(1))
        {
            throw new ValidationException("EndedAtUtc cannot be in the future.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            return await BuildCloseSessionResponseAsync(session, false, ct);
        }

        if (endedAtUtc <= session.StartedAtUtc)
        {
            throw new ValidationException("EndedAtUtc must be after session start time.");
        }

        var closedTableIds = await CloseActiveAssignmentsAsync(session, endedAtUtc, closedByUserId, ct);
        var timeCharge = await CalculateSessionTimeChargeAsync(sessionId, endedAtUtc, true, ct);

        var totalDurationMinutes = timeCharge.ActualDurationMinutes;
        var timeSubtotal = timeCharge.SubtotalAmount;
        var productSubtotal = await db.Orders
            .Where(x => x.SessionId == sessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);
        await db.SaveChangesAsync(ct);
        EntityInvoice? invoice = request.GenerateInvoice
            ? await CreateOrGetSessionInvoiceAsync(session, closedByUserId, ct)
            : null;

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        foreach (var tableId in closedTableIds)
        {
            await posNotificationService.NotifyTableUpdateAsync((int)tableId, ct);
        }
        await posNotificationService.NotifySessionClosedAsync((int)session.SessionId, closedTableIds.FirstOrDefault() is long closedTableId ? (int)closedTableId : null, ct);

        return new CloseSessionResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = endedAtUtc,
            TotalDurationMinutes = totalDurationMinutes,
            TimeSubtotalAmount = timeSubtotal,
            ProductSubtotalAmount = productSubtotal,
            SubtotalAmount = timeSubtotal + productSubtotal,
            DiscountAmount = invoice?.DiscountAmount ?? 0,
            GrandTotalAmount = invoice?.GrandTotalAmount ?? timeSubtotal + productSubtotal,
            InvoiceId = invoice?.InvoiceId,
            InvoiceCode = invoice?.InvoiceCode,
            InvoiceGenerated = invoice is not null
        };
    }

    private async Task<CloseSessionResponse> BuildCloseSessionResponseAsync(EntitySession session, bool invoiceGenerated, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var timeCharge = await CalculateSessionTimeChargeAsync(session.SessionId, session.EndedAtUtc ?? now, false, ct);
        var invoice = await db.Invoices.AsNoTracking()
            .Where(x => x.SessionId == session.SessionId)
            .OrderByDescending(x => x.InvoiceId)
            .FirstOrDefaultAsync(ct);
        var productSubtotal = await db.Orders.AsNoTracking()
            .Where(x => x.SessionId == session.SessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);
        var timeSubtotal = timeCharge.SubtotalAmount;
        var subtotal = timeSubtotal + productSubtotal;

        return new CloseSessionResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc ?? now,
            TotalDurationMinutes = timeCharge.ActualDurationMinutes,
            TimeSubtotalAmount = invoice?.TimeSubtotalAmount ?? timeSubtotal,
            ProductSubtotalAmount = invoice?.ProductSubtotalAmount ?? productSubtotal,
            SubtotalAmount = invoice?.SubtotalAmount ?? subtotal,
            DiscountAmount = invoice?.DiscountAmount ?? 0,
            GrandTotalAmount = invoice?.GrandTotalAmount ?? subtotal,
            InvoiceId = invoice?.InvoiceId,
            InvoiceCode = invoice?.InvoiceCode,
            InvoiceGenerated = invoiceGenerated || invoice is not null
        };
    }

    public async Task<ReleaseSessionTablesResponse> ReleaseTablesAsync(long sessionId, ReleaseSessionTablesRequest request, long performedByUserId, CancellationToken ct)
    {
        request ??= new ReleaseSessionTablesRequest();
        var assignmentIds = request.AssignmentIds.Distinct().ToList();
        if (assignmentIds.Count == 0)
        {
            throw new ValidationException("AssignmentIds is required.");
        }

        var now = _clock.UtcNow;
        var endedAtUtc = request.EndedAtUtc ?? now;
        if (endedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException("EndedAtUtc must use UTC.");
        }
        if (endedAtUtc > now.AddMinutes(1))
        {
            throw new ValidationException("EndedAtUtc cannot be in the future.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException("Only active sessions can release tables.");
        }

        var assignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && assignmentIds.Contains(x.SessionTableAssignmentId))
            .ToListAsync(ct);
        if (assignments.Count != assignmentIds.Count)
        {
            throw new NotFoundException("One or more assignments were not found in this session.");
        }
        if (assignments.Any(x => x.EndedAtUtc.HasValue))
        {
            throw new ConflictException("One or more assignments are already released.");
        }

        foreach (var assignment in assignments)
        {
            await CloseAssignmentAsync(assignment, endedAtUtc, request.Note, ct);
        }

        await db.SaveChangesAsync(ct);

        var remainingActive = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .CountAsync(ct);

        EntityInvoice? invoice = null;
        var wasAutoClosed = remainingActive == 0;
        if (wasAutoClosed)
        {
            session.Status = 2;
            session.EndedAtUtc = endedAtUtc;
            session.ClosedByUserId = performedByUserId;

            if (session.BookingId.HasValue)
            {
                var booking = await db.Bookings.FindAsync([session.BookingId.Value], ct);
                if (booking is not null)
                {
                    booking.Status = BookingStatuses.Completed;
                }
            }

            invoice = await CreateOrGetSessionInvoiceAsync(session, performedByUserId, ct);
        }

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        foreach (var tableId in assignments.Select(x => x.TableId).Distinct())
        {
            await posNotificationService.NotifyTableUpdateAsync((int)tableId, ct);
        }
        if (wasAutoClosed)
        {
            await posNotificationService.NotifySessionClosedAsync((int)session.SessionId, assignments.FirstOrDefault()?.TableId is long tableId ? (int)tableId : null, ct);
        }
        else
        {
            await posNotificationService.NotifySessionUpdateAsync((int)session.SessionId, ct);
        }

        var releasedIds = assignments.Select(x => x.SessionTableAssignmentId).ToList();
        var released = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                              join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                              where releasedIds.Contains(assignment.SessionTableAssignmentId)
                              orderby assignment.SessionTableAssignmentId
                              select new ReleasedSessionTableDto
                              {
                                  AssignmentId = assignment.SessionTableAssignmentId,
                                  TableId = assignment.TableId,
                                  TableCode = table.TableCode,
                                  TableName = table.TableName,
                                  StartedAtUtc = assignment.StartedAtUtc,
                                  EndedAtUtc = assignment.EndedAtUtc ?? endedAtUtc,
                                  DurationMinutes = assignment.DurationMinutes ?? 0,
                                  HourlyRateSnapshot = assignment.HourlyRateSnapshot,
                                  Amount = assignment.Amount ?? 0
                              })
            .ToListAsync(ct);

        var remainingAssignments = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                                          join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                                          join zone in db.Zones.AsNoTracking() on table.ZoneId equals zone.ZoneId
                                          where assignment.SessionId == sessionId && assignment.EndedAtUtc == null
                                          orderby assignment.StartedAtUtc, assignment.SessionTableAssignmentId
                                          select new ActiveSessionTableDto
                                          {
                                              AssignmentId = assignment.SessionTableAssignmentId,
                                              TableId = assignment.TableId,
                                              TableCode = table.TableCode,
                                              TableName = table.TableName,
                                              ZoneId = table.ZoneId,
                                              FloorId = zone.FloorId,
                                              StartedAtUtc = assignment.StartedAtUtc,
                                              HourlyRateSnapshot = assignment.HourlyRateSnapshot
                                          })
            .ToListAsync(ct);

        var timeCharge = await CalculateSessionTimeChargeAsync(sessionId, endedAtUtc, false, ct);
        return new ReleaseSessionTablesResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            SessionStatus = session.Status,
            ReleasedAssignments = released,
            RemainingActiveAssignments = remainingAssignments,
            RemainingActiveTableCount = remainingActive,
            WasSessionAutoClosed = wasAutoClosed,
            SessionEndedAtUtc = session.EndedAtUtc,
            TimeSubtotalAmount = timeCharge.SubtotalAmount,
            InvoiceId = invoice?.InvoiceId,
            InvoiceCode = invoice?.InvoiceCode,
            Message = wasAutoClosed ? "Last table released. Session closed and invoice generated." : "Selected table assignments released."
        };
    }

    public async Task<SessionDto> CancelAsync(long sessionId, long? cancelledByUserId, CancelSessionRequest request, CancellationToken ct)
    {
        request ??= new CancelSessionRequest();

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            throw new ConflictException("Closed sessions cannot be cancelled.");
        }

        if (session.Status == 3)
        {
            throw new ConflictException("Session is already cancelled.");
        }

        if (session.Status != 1)
        {
            throw new BusinessRuleException("Only active sessions can be cancelled.");
        }

        var hasPayment = await (from invoice in db.Invoices
                                join payment in db.Payments on invoice.InvoiceId equals payment.InvoiceId
                                where invoice.SessionId == sessionId
                                select payment.PaymentId)
            .AnyAsync(ct);
        if (hasPayment)
        {
            throw new ConflictException("Session already has a payment and cannot be cancelled.");
        }

        var hasInvoice = await db.Invoices.AnyAsync(x => x.SessionId == sessionId, ct);
        if (hasInvoice)
        {
            throw new ConflictException("Session already has an invoice and cannot be cancelled.");
        }

        var endedAtUtc = _clock.UtcNow;
        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);

        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = endedAtUtc;
            assignment.DurationMinutes = 0;
            assignment.Amount = 0;
        }

        session.Status = 3; // Cancelled
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = cancelledByUserId;
        session.Note = string.IsNullOrWhiteSpace(request.Reason) ? session.Note : request.Reason.Trim();

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        foreach (var assignment in activeAssignments)
        {
            await posNotificationService.NotifyTableUpdateAsync((int)assignment.TableId, ct);
        }
        await posNotificationService.NotifySessionUpdateAsync((int)session.SessionId, ct);

        return new SessionDto
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            Status = session.Status
        };
    }

    public async Task<TransferTableResponse> TransferTableAsync(long sessionId, TransferTableRequest request, long? assignedByUserId, CancellationToken ct)
    {
        request ??= new TransferTableRequest();
        if (request.SourceAssignmentId <= 0)
        {
            throw new ValidationException("SourceAssignmentId is required.");
        }
        var newTableId = request.TargetTableId;
        if (newTableId <= 0)
        {
            throw new ValidationException("ToTableId is required.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException(session.Status == 2
                ? "Session is closed. Reopen the session before transferring tables."
                : "Only active sessions can be transferred.");
        }

        var currentAssignment = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.SessionTableAssignmentId == request.SourceAssignmentId && x.EndedAtUtc == null)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Active assignment not found.");

        if (currentAssignment.TableId == newTableId)
        {
            throw new BusinessRuleException("Cannot transfer to the current table.");
        }

        var currentTable = await db.VenueTables.FindAsync([currentAssignment.TableId], ct)
            ?? throw new NotFoundException("Current table not found.");

        var hasActiveSessionNewTable = await db.SessionTableAssignments
            .AnyAsync(sta => sta.TableId == newTableId && sta.EndedAtUtc == null && db.Sessions.Any(s => s.SessionId == sta.SessionId && s.Status == 1), ct);
        if (hasActiveSessionNewTable)
        {
            throw new ConflictException("New table already has an active session.");
        }

        var newTable = await db.VenueTables.FindAsync([newTableId], ct) ?? throw new NotFoundException("New table not found.");
        if (!newTable.IsActive || newTable.OperationalStatus is TableOperationalStatuses.Maintenance or TableOperationalStatuses.Inactive)
        {
            throw new BusinessRuleException("New table is not available.");
        }

        var endedAtUtc = request.TransferAtUtc ?? _clock.UtcNow;
        if (endedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ValidationException("TransferAtUtc must use UTC.");
        }
        if (endedAtUtc <= currentAssignment.StartedAtUtc)
        {
            throw new ValidationException("TransferAtUtc must be after source assignment start time.");
        }
        await EnsureNoUpcomingBookingConflictAsync(newTableId, endedAtUtc, 15, ct);
        await EnsureSessionHasNoPaidInvoiceAsync(sessionId, ct);
        await CancelOpenInvoicesForSessionAsync(sessionId, "Invoice cancelled because the active session was transferred to another table.", ct);

        await CloseAssignmentAsync(currentAssignment, endedAtUtc, BuildTransferNote(request.Reason, request.Note), ct);
        if (request.MarkOldTableMaintenance)
        {
            currentTable.OperationalStatus = TableOperationalStatuses.Maintenance;
            currentTable.UpdatedAtUtc = endedAtUtc;
        }

        var rule = await FindActiveRuleAsync(newTable.TableTypeId, endedAtUtc, ct)
            ?? throw BuildMissingPricingRuleException(newTable, endedAtUtc, "thá»i Ä‘iá»ƒm chuyá»ƒn bĂ n");
        var hourlyRate = rule.HourlyRate;

        var newAssignment = new SessionTableAssignment
        {
            SessionId = sessionId,
            TableId = newTableId,
            StartedAtUtc = endedAtUtc,
            AssignedByUserId = assignedByUserId,
            PricingPlanRuleId = rule?.PricingPlanRuleId,
            HourlyRateSnapshot = hourlyRate
        };
        db.SessionTableAssignments.Add(newAssignment);

        await db.SaveChangesAsync(ct);
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = assignedByUserId,
            Action = "SessionTransferTable",
            EntityName = nameof(EntitySession),
            EntityId = sessionId,
            OldValues = JsonSerializer.Serialize(new { tableId = currentTable.TableId, tableName = currentTable.TableName }),
            NewValues = JsonSerializer.Serialize(new { tableId = newTable.TableId, tableName = newTable.TableName, request.Reason, request.Note, request.MarkOldTableMaintenance }),
            Description = $"Session {session.SessionCode} transferred from {currentTable.TableName} to {newTable.TableName}."
        });
        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        await posNotificationService.NotifySessionTransferredAsync((int)sessionId, (int)currentAssignment.TableId, (int)newTableId, ct);

        return new TransferTableResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            FromTableId = currentTable.TableId,
            FromTableName = currentTable.TableName,
            ToTableId = newTable.TableId,
            ToTableName = newTable.TableName,
            TransferredAtUtc = endedAtUtc,
            CurrentAssignmentId = newAssignment.SessionTableAssignmentId,
            Message = "Chuyá»ƒn bĂ n thĂ nh cĂ´ng"
        };
    }

    public async Task<SessionDetailDto> ReopenAsync(long sessionId, ReopenSessionRequest request, long? reopenedByUserId, CancellationToken ct)
    {
        request ??= new ReopenSessionRequest();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("Reason is required.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 3)
        {
            throw new BusinessRuleException("Cancelled sessions cannot be reopened.");
        }

        if (session.Status != 2)
        {
            throw new BusinessRuleException("Only closed sessions can be reopened.");
        }

        await EnsureSessionHasNoPaidInvoiceAsync(sessionId, ct);

        var lastAssignment = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Session has no table assignment.");
        var assignmentCount = await db.SessionTableAssignments.CountAsync(x => x.SessionId == sessionId, ct);
        if (request.ReopenLastTable && assignmentCount != 1)
        {
            throw new ConflictException("Multi-table or transferred sessions cannot reopen a single last table safely.");
        }

        var table = await db.VenueTables.FindAsync([lastAssignment.TableId], ct) ?? throw new NotFoundException("Last table not found.");
        var now = _clock.UtcNow;

        if (request.ReopenLastTable)
        {
            if (!table.IsActive || table.OperationalStatus is TableOperationalStatuses.Maintenance or TableOperationalStatuses.Inactive)
            {
                throw new ConflictException("Last table is inactive or under maintenance and cannot be reopened.");
            }

            await EnsureTableHasNoActiveSessionAsync(table.TableId, "Last table already has another active session.", ct);
            await EnsureNoUpcomingBookingConflictAsync(table.TableId, now, 15, ct);

            lastAssignment.EndedAtUtc = null;
            lastAssignment.DurationMinutes = null;
            lastAssignment.Amount = null;
            lastAssignment.Note = BuildReopenNote(lastAssignment.Note, request.Reason);
        }

        await CancelOpenInvoicesForSessionAsync(sessionId, $"Invoice cancelled because session was reopened. Reason: {request.Reason.Trim()}", ct);

        session.Status = 1;
        session.EndedAtUtc = null;
        session.ClosedByUserId = null;
        session.UpdatedAtUtc = now;
        session.Note = BuildReopenNote(session.Note, request.Reason);

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = reopenedByUserId,
            Action = "SessionReopen",
            EntityName = nameof(EntitySession),
            EntityId = sessionId,
            NewValues = JsonSerializer.Serialize(new { request.Reason, request.ReopenLastTable, tableId = table.TableId }),
            Description = $"Session {session.SessionCode} reopened. Reason: {request.Reason.Trim()}"
        });

        await db.SaveChangesAsync(ct);
        if (transaction is not null)
        {
            await transaction.CommitAsync(ct);
        }

        await posNotificationService.NotifyTableUpdateAsync((int)table.TableId, ct);
        await posNotificationService.NotifySessionUpdateAsync((int)sessionId, ct);

        return await GetSessionByIdAsync(sessionId, ct);
    }

    private async Task<SessionTimeChargeSummaryDto> CalculateSessionTimeChargeAsync(long sessionId, DateTime previewEndedAtUtc, bool persist, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
            ?? throw new NotFoundException("Session not found.");

        var rows = await (from assignment in db.SessionTableAssignments
                          join table in db.VenueTables on assignment.TableId equals table.TableId
                          where assignment.SessionId == sessionId
                          orderby assignment.StartedAtUtc, assignment.SessionTableAssignmentId
                          select new { Assignment = assignment, Table = table })
            .ToListAsync(ct);

        var lines = new List<SessionSummaryAssignmentDto>();
        foreach (var row in rows)
        {
            var isCurrent = row.Assignment.EndedAtUtc == null && session.Status == 1;
            var endedAtUtc = row.Assignment.EndedAtUtc ?? previewEndedAtUtc;
            var actualMinutes = GetDurationMinutes(row.Assignment.StartedAtUtc, endedAtUtc, persist ? null : row.Assignment.DurationMinutes);
            var pricing = await GetAssignmentPricingAsync(row.Assignment, row.Table, ct);
            var isBillable = IsAssignmentBillable(row.Assignment, actualMinutes);
            var billableMinutes = isBillable ? ApplyBillingRules(actualMinutes, pricing.MinimumMinutes, pricing.BillingBlockMinutes) : 0;
            var hourlyRate = row.Assignment.HourlyRateSnapshot > 0 ? row.Assignment.HourlyRateSnapshot : pricing.HourlyRate;
            var amount = row.Assignment.EndedAtUtc.HasValue && row.Assignment.Amount.HasValue
                ? row.Assignment.Amount.Value
                : Math.Round(((decimal)billableMinutes / 60m) * hourlyRate, 2, MidpointRounding.AwayFromZero);
            var note = isBillable ? row.Assignment.Note : "Free due to table issue/staff correction grace.";

            lines.Add(new SessionSummaryAssignmentDto
            {
                SessionTableAssignmentId = row.Assignment.SessionTableAssignmentId,
                TableId = row.Assignment.TableId,
                TableCode = row.Table.TableCode,
                TableName = row.Table.TableName,
                PricingPlanRuleId = row.Assignment.PricingPlanRuleId ?? pricing.PricingPlanRuleId,
                StartedAtUtc = row.Assignment.StartedAtUtc,
                EndedAtUtc = row.Assignment.EndedAtUtc,
                DurationMinutes = actualMinutes,
                ActualDurationMinutes = actualMinutes,
                BillableDurationMinutes = billableMinutes,
                HourlyRateSnapshot = hourlyRate,
                HourlyRate = hourlyRate,
                MinimumMinutes = pricing.MinimumMinutes,
                BillingBlockMinutes = pricing.BillingBlockMinutes,
                PricingPlanName = pricing.PricingPlanName,
                Amount = amount,
                IsCurrent = isCurrent,
                IsBillable = isBillable,
                Note = note
            });
        }

        foreach (var line in lines)
        {
            if (!persist)
            {
                continue;
            }

            var assignment = rows.First(x => x.Assignment.SessionTableAssignmentId == line.SessionTableAssignmentId).Assignment;
            if (assignment.EndedAtUtc.HasValue && assignment.Amount.HasValue)
            {
                continue;
            }

            assignment.DurationMinutes = line.ActualDurationMinutes;
            assignment.Amount = line.Amount;
            assignment.HourlyRateSnapshot = line.HourlyRate;
            assignment.PricingPlanRuleId = line.PricingPlanRuleId;
            assignment.Note = line.Note;
        }

        var ruleLine = lines.FirstOrDefault(x => x.IsBillable) ?? lines.FirstOrDefault();
        return new SessionTimeChargeSummaryDto
        {
            ActualDurationMinutes = lines.Sum(x => x.ActualDurationMinutes),
            BillableDurationMinutes = lines.Sum(x => x.BillableDurationMinutes),
            MinimumMinutes = ruleLine?.MinimumMinutes ?? 0,
            BillingBlockMinutes = ruleLine?.BillingBlockMinutes ?? 0,
            SubtotalAmount = lines.Sum(x => x.Amount),
            Note = "Minimum/block applies separately to each table assignment.",
            Lines = lines
        };
    }
    private async Task<List<SessionSummaryOrderDto>> GetSessionSummaryOrdersAsync(long sessionId, CancellationToken ct)
    {
        var orders = await db.Orders.AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.Status != 3)
            .OrderBy(x => x.OrderId)
            .ToListAsync(ct);
        var orderIds = orders.Select(x => x.OrderId).ToList();
        var items = await db.OrderItems.AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .OrderBy(x => x.OrderItemId)
            .ToListAsync(ct);

        return orders.Select(order => new SessionSummaryOrderDto
        {
            OrderId = order.OrderId,
            OrderCode = order.OrderCode,
            Status = order.Status,
            SubtotalAmount = order.SubtotalAmount,
            Items = items.Where(item => item.OrderId == order.OrderId)
                .Select(item => new SessionSummaryOrderItemDto
                {
                    OrderItemId = item.OrderItemId,
                    ProductName = item.ProductNameSnapshot,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPriceSnapshot,
                    LineTotalAmount = item.LineTotalAmount
                })
                .ToList()
        }).ToList();
    }

    private async Task<PricingCharge> GetAssignmentPricingAsync(SessionTableAssignment assignment, VenueTable table, CancellationToken ct)
    {
        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct)
            ?? throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thá»i Ä‘iá»ƒm báº¯t Ä‘áº§u gĂ¡n bĂ n");
        var pricingPlanName = await db.PricingPlans.AsNoTracking()
            .Where(x => x.PricingPlanId == rule.PricingPlanId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        return new PricingCharge(
            0,
            0,
            rule.PricingPlanRuleId,
            assignment.HourlyRateSnapshot > 0 ? assignment.HourlyRateSnapshot : rule.HourlyRate,
            rule.MinimumMinutes,
            rule.BillingBlockMinutes,
            pricingPlanName);
    }

    private static bool IsAssignmentBillable(SessionTableAssignment assignment, int actualMinutes)
    {
        const int transferGraceMinutes = 5;
        if (actualMinutes > transferGraceMinutes || string.IsNullOrWhiteSpace(assignment.Note))
        {
            return true;
        }

        return !assignment.Note.Contains("TableIssue", StringComparison.OrdinalIgnoreCase) &&
               !assignment.Note.Contains("StaffCorrection", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<PricingPlanRule?> FindActiveRuleAsync(long tableTypeId, DateTime time, CancellationToken ct)
    {
        var utcTime = NormalizeUtc(time);
        var localTime = ConvertUtcToVenueLocal(utcTime);
        var dayOfWeek = (int)localTime.DayOfWeek;
        var previousDayOfWeek = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        var timeOfDay = localTime.TimeOfDay;

        var activePlans = await db.PricingPlans
            .Where(p => p.IsActive && p.StartsAtUtc <= utcTime && (p.EndsAtUtc == null || p.EndsAtUtc >= utcTime))
            .ToListAsync(ct);

        if (!activePlans.Any()) return null;

        var planIds = activePlans.OrderByDescending(p => p.IsDefault).Select(p => p.PricingPlanId).ToList();

        foreach (var planId in planIds)
        {
            var candidates = await db.PricingPlanRules
                .Where(r => r.PricingPlanId == planId &&
                            r.TableTypeId == tableTypeId &&
                            r.IsActive &&
                            (r.DayOfWeek == dayOfWeek || r.DayOfWeek == previousDayOfWeek))
                .OrderByDescending(r => r.DayOfWeek == dayOfWeek)
                .ThenByDescending(r => r.PricingPlanRuleId)
                .ToListAsync(ct);
            var rule = candidates.FirstOrDefault(r => RuleMatchesLocalTime(r, dayOfWeek, timeOfDay));
            if (rule != null) return rule;
        }

        return null;
    }

    private static bool RuleMatchesLocalTime(PricingPlanRule rule, int localDayOfWeek, TimeSpan localTime)
    {
        if (rule.StartTime < rule.EndTime)
        {
            return rule.DayOfWeek == localDayOfWeek &&
                   rule.StartTime <= localTime &&
                   localTime < rule.EndTime;
        }

        if (rule.StartTime > rule.EndTime)
        {
            return (rule.DayOfWeek == localDayOfWeek && localTime >= rule.StartTime) ||
                   (NextDay(rule.DayOfWeek) == localDayOfWeek && localTime < rule.EndTime);
        }

        return rule.DayOfWeek == localDayOfWeek;
    }

    private static int NextDay(int dayOfWeek) => dayOfWeek == 6 ? 0 : dayOfWeek + 1;

    private ConflictException BuildMissingPricingRuleException(VenueTable table, DateTime startedAtUtc, string context)
    {
        var utcTime = NormalizeUtc(startedAtUtc);
        var localTime = ConvertUtcToVenueLocal(utcTime);
        var message = $"KhĂ´ng tĂ¬m tháº¥y báº£ng giĂ¡ Ä‘ang Ă¡p dá»¥ng cho bĂ n {table.TableCode} táº¡i {context}.";
        return new ConflictException(message, [
            $"tableCode={table.TableCode}",
            $"tableTypeId={table.TableTypeId}",
            $"startedAtUtc={utcTime:O}",
            $"venueLocalTime={localTime:O}",
            $"dayOfWeek={(int)localTime.DayOfWeek}",
            $"localTime={localTime.TimeOfDay}"
        ]);
    }

    private DateTime ConvertUtcToVenueLocal(DateTime utcTime)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(NormalizeUtc(utcTime), GetVenueTimeZone());
    }

    private TimeZoneInfo GetVenueTimeZone()
    {
        var configuredId = config?["Venue:TimeZoneId"] ?? "Asia/Ho_Chi_Minh";
        foreach (var id in new[] { configuredId, "Asia/Ho_Chi_Minh", "SE Asia Standard Time" }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static DateTime NormalizeUtc(DateTime time) =>
        time.Kind == DateTimeKind.Utc ? time : DateTime.SpecifyKind(time, DateTimeKind.Utc);

    private async Task EnsureSessionHasNoPaidInvoiceAsync(long sessionId, CancellationToken ct)
    {
        var hasPaidInvoice = await db.Invoices.AnyAsync(x =>
            x.SessionId == sessionId &&
            x.Status != 3 &&
            (x.PaymentStatus == InvoicePaymentStatuses.Paid ||
             x.PaymentStatus == InvoicePaymentStatuses.PartiallyPaid ||
             x.PaidAmount > 0), ct);

        if (hasPaidInvoice)
        {
            throw new ConflictException("Session has paid or partially paid invoice and cannot be changed.");
        }

        var hasCompletedPayment = await (from invoice in db.Invoices
                                         join payment in db.Payments on invoice.InvoiceId equals payment.InvoiceId
                                         where invoice.SessionId == sessionId &&
                                               invoice.Status != 3 &&
                                               payment.PaymentStatus == PaymentStatuses.Completed
                                         select payment.PaymentId)
            .AnyAsync(ct);

        if (hasCompletedPayment)
        {
            throw new ConflictException("Session has completed payment and cannot be changed.");
        }
    }

    private async Task CancelOpenInvoicesForSessionAsync(long sessionId, string reason, CancellationToken ct)
    {
        var invoices = await db.Invoices
            .Where(x => x.SessionId == sessionId && x.Status != 3 && x.PaymentStatus == InvoicePaymentStatuses.Unpaid && x.PaidAmount == 0)
            .ToListAsync(ct);

        foreach (var invoice in invoices)
        {
            invoice.Status = 3;
            invoice.Note = string.IsNullOrWhiteSpace(invoice.Note)
                ? reason
                : $"{invoice.Note.Trim()} | {reason}";
            invoice.UpdatedAtUtc = _clock.UtcNow;
        }
    }

    private async Task EnsureNoUpcomingBookingConflictAsync(long tableId, DateTime atUtc, int bufferMinutes, CancellationToken ct)
    {
        var windowEndUtc = atUtc.AddMinutes(Math.Max(15, bufferMinutes));
        var conflict = await db.Bookings.AsNoTracking()
            .Where(x => x.TableId == tableId &&
                        (x.Status == BookingStatuses.Confirmed || x.Status == BookingStatuses.NoShow) &&
                        x.EndTimeUtc > atUtc &&
                        x.StartTimeUtc < windowEndUtc)
            .OrderBy(x => x.StartTimeUtc)
            .Select(x => new { x.BookingCode, x.StartTimeUtc, x.EndTimeUtc })
            .FirstOrDefaultAsync(ct);

        if (conflict is not null)
        {
            throw new ConflictException($"Target table has booking conflict: {conflict.BookingCode} from {conflict.StartTimeUtc:O} to {conflict.EndTimeUtc:O}.");
        }
    }

    private static string? BuildTransferNote(string? reason, string? note)
    {
        var parts = new[] { reason, note }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim());
        var value = string.Join(" | ", parts);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string BuildReopenNote(string? currentNote, string reason)
    {
        var entry = $"Reopened: {reason.Trim()}";
        return string.IsNullOrWhiteSpace(currentNote) ? entry : $"{currentNote.Trim()} | {entry}";
    }

    private async Task CalculateAssignmentAmountAsync(SessionTableAssignment assignment, DateTime endedAtUtc, CancellationToken ct)
    {
        assignment.EndedAtUtc = endedAtUtc;
        var table = await db.VenueTables.FindAsync([assignment.TableId], ct);
        if (table == null) return;

        // Set table status back to 1 (Available)
        table.OperationalStatus = 1; 

        var durationMinutes = (int)Math.Ceiling((endedAtUtc - assignment.StartedAtUtc).TotalMinutes);
        if (durationMinutes < 0) durationMinutes = 0;
        assignment.DurationMinutes = durationMinutes;

        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct);
        if (rule != null)
        {
            assignment.PricingPlanRuleId = rule.PricingPlanRuleId;
            assignment.HourlyRateSnapshot = rule.HourlyRate;

            var billableMinutes = durationMinutes;
            if (billableMinutes < rule.MinimumMinutes)
            {
                billableMinutes = rule.MinimumMinutes;
            }
            if (rule.BillingBlockMinutes > 0)
            {
                var remainder = billableMinutes % rule.BillingBlockMinutes;
                if (remainder > 0)
                {
                    billableMinutes += (rule.BillingBlockMinutes - remainder);
                }
            }
            assignment.Amount = ((decimal)billableMinutes / 60m) * rule.HourlyRate;
        }
        else
        {
            throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thá»i Ä‘iá»ƒm báº¯t Ä‘áº§u gĂ¡n bĂ n");
        }
    }

    private async Task<PricingCharge> CalculatePreviewChargeAsync(SessionTableAssignment assignment, VenueTable table, int durationMinutes, CancellationToken ct)
    {
        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct);
        if (rule is null)
        {
            throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thá»i Ä‘iá»ƒm báº¯t Ä‘áº§u gĂ¡n bĂ n");
        }

        var billableMinutes = durationMinutes;
        if (billableMinutes < rule.MinimumMinutes)
        {
            billableMinutes = rule.MinimumMinutes;
        }

        if (rule.BillingBlockMinutes > 0)
        {
            var remainder = billableMinutes % rule.BillingBlockMinutes;
            if (remainder > 0)
            {
                billableMinutes += rule.BillingBlockMinutes - remainder;
            }
        }

        var pricingPlanName = await db.PricingPlans.AsNoTracking()
            .Where(x => x.PricingPlanId == rule.PricingPlanId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        return new PricingCharge(
            billableMinutes,
            ((decimal)billableMinutes / 60m) * rule.HourlyRate,
            rule.PricingPlanRuleId,
            rule.HourlyRate,
            rule.MinimumMinutes,
            rule.BillingBlockMinutes,
            pricingPlanName);
    }

    private static int GetDurationMinutes(DateTime startedAtUtc, DateTime endedAtUtc, int? storedDurationMinutes = null)
    {
        if (storedDurationMinutes.HasValue)
        {
            return Math.Max(0, storedDurationMinutes.Value);
        }

        return Math.Max(0, (int)Math.Ceiling((endedAtUtc - startedAtUtc).TotalMinutes));
    }

    private async Task CalculateAssignmentAmountStrictAsync(SessionTableAssignment assignment, VenueTable table, DateTime endedAtUtc, CancellationToken ct)
    {
        assignment.EndedAtUtc = endedAtUtc;

        var durationMinutes = (int)Math.Ceiling((endedAtUtc - assignment.StartedAtUtc).TotalMinutes);
        if (durationMinutes <= 0)
        {
            throw new ValidationException("Assignment duration must be greater than zero.");
        }

        assignment.DurationMinutes = durationMinutes;

        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct)
            ?? throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thá»i Ä‘iá»ƒm má»Ÿ phiĂªn");

        assignment.PricingPlanRuleId = rule.PricingPlanRuleId;
        assignment.HourlyRateSnapshot = rule.HourlyRate;

        var billableMinutes = durationMinutes;
        if (billableMinutes < rule.MinimumMinutes)
        {
            billableMinutes = rule.MinimumMinutes;
        }

        if (rule.BillingBlockMinutes > 0)
        {
            var remainder = billableMinutes % rule.BillingBlockMinutes;
            if (remainder > 0)
            {
                billableMinutes += rule.BillingBlockMinutes - remainder;
            }
        }

        assignment.Amount = ((decimal)billableMinutes / 60m) * rule.HourlyRate;
    }

    private async Task ApplyBookingDepositToInvoiceAsync(EntitySession session, EntityInvoice invoice, CancellationToken ct)
    {
        if (!session.BookingId.HasValue) return;

        var deposit = await db.BookingDeposits.FirstOrDefaultAsync(x =>
            x.BookingId == session.BookingId.Value &&
            x.Status == BookingDepositStatuses.Paid &&
            x.PaidAmount > 0, ct);
        if (deposit is null) return;

        var appliedAmount = Math.Min(deposit.PaidAmount, invoice.GrandTotalAmount);
        if (appliedAmount <= 0) return;

        var paymentMethod = await db.PaymentMethods.FirstOrDefaultAsync(x => x.Code == "DEPOSIT", ct);
        if (paymentMethod is null)
        {
            paymentMethod = new PaymentMethod
            {
                Code = "DEPOSIT",
                Name = "Deposit Applied",
                Description = "System payment method used when applying booking deposits to invoices.",
                IsActive = true
            };
            db.PaymentMethods.Add(paymentMethod);
            await db.SaveChangesAsync(ct);
        }

        if (await db.Payments.AnyAsync(x => x.InvoiceId == invoice.InvoiceId && x.PaymentMethodId == paymentMethod.PaymentMethodId, ct))
            return;

        db.Payments.Add(new Payment
        {
            InvoiceId = invoice.InvoiceId,
            PaymentMethodId = paymentMethod.PaymentMethodId,
            Amount = appliedAmount,
            PaymentStatus = PaymentStatuses.Completed,
            TransactionCode = $"DEPAPP{_clock.UtcNow:HHmmssddMMyyyy}",
            PaidAtUtc = _clock.UtcNow,
            Note = $"Booking deposit applied from booking #{session.BookingId.Value}"
        });

        invoice.PaidAmount += appliedAmount;
        invoice.PaymentStatus = invoice.PaidAmount >= invoice.GrandTotalAmount
            ? InvoicePaymentStatuses.Paid
            : InvoicePaymentStatuses.PartiallyPaid;
        if (invoice.PaymentStatus == InvoicePaymentStatuses.Paid)
        {
            invoice.Status = 2;
            await ProcessInvoicePaidRewardsAsync(invoice, ct);
        }

        deposit.AppliedAmount = appliedAmount;
        deposit.AppliedToInvoiceId = invoice.InvoiceId;
        if (deposit.PaidAmount > invoice.GrandTotalAmount)
        {
            deposit.RefundedAmount = deposit.PaidAmount - invoice.GrandTotalAmount;
            deposit.RefundedAtUtc = _clock.UtcNow;
            deposit.Status = BookingDepositStatuses.PartiallyRefunded;
        }
        else
        {
            deposit.Status = BookingDepositStatuses.AppliedToInvoice;
        }
    }

    private async Task ProcessInvoicePaidRewardsAsync(EntityInvoice invoice, CancellationToken ct)
    {
        var appliedDiscounts = await db.InvoiceDiscounts.Where(x => x.InvoiceId == invoice.InvoiceId).ToListAsync(ct);
        foreach (var ad in appliedDiscounts)
        {
            var disc = await db.Discounts.FindAsync([ad.DiscountId], ct);
            if (disc != null)
            {
                disc.UsageCount++;
                if (disc.MaxUsage > 0 && disc.UsageCount >= disc.MaxUsage)
                {
                    disc.IsActive = false;
                }
                db.Discounts.Update(disc);
            }
        }

        if (invoice.CustomerId.HasValue)
        {
            var customer = await db.Customers.FindAsync([invoice.CustomerId.Value], ct);
            if (customer != null && customer.Status)
            {
                int earnedPoints = (int)(invoice.GrandTotalAmount / 1000m);
                if (earnedPoints > 0)
                {
                    customer.LoyaltyPoints += earnedPoints;
                    customer.TotalPointsEarned += earnedPoints;
                    db.Customers.Update(customer);

                    db.CustomerPointHistories.Add(new CustomerPointHistory
                    {
                        CustomerId = customer.CustomerId,
                        Points = earnedPoints,
                        TransactionType = "EARN",
                        Description = $"TĂ­ch Ä‘iá»ƒm tá»« hĂ³a Ä‘Æ¡n {invoice.InvoiceCode} ({invoice.GrandTotalAmount:N0} VND)",
                        ReferenceId = invoice.InvoiceId,
                        CreatedAtUtc = _clock.UtcNow
                    });
                }
            }
        }
    }

    private sealed record PricingCharge(
        int BillableMinutes,
        decimal Amount,
        long PricingPlanRuleId,
        decimal HourlyRate,
        int MinimumMinutes,
        int BillingBlockMinutes,
        string? PricingPlanName);

    private sealed class NoOpPosNotificationService : IPosNotificationService
    {
        public Task NotifyTableUpdateAsync(int tableId, CancellationToken ct = default) => Task.CompletedTask;

        public Task NotifyBookingUpdateAsync(int bookingId, CancellationToken ct = default) => Task.CompletedTask;

        public Task NotifySessionUpdateAsync(int sessionId, CancellationToken ct = default) => Task.CompletedTask;

        public Task NotifyRefreshPosAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
