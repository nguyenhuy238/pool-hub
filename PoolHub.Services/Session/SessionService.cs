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
using System.Text.Json;
using EntitySession = PoolHub.Core.Entities.Session;
using EntityInvoice = PoolHub.Core.Entities.Invoice;

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
            TableName = !string.IsNullOrEmpty(x.TableName) ? x.TableName : $"Bàn #{x.SessionId}",
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
                    TableId = x.Table.TableId,
                    TableCode = x.Table.TableCode,
                    TableName = x.Table.TableName,
                    ZoneId = x.Table.ZoneId,
                    FloorId = x.Zone.FloorId
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
                : "Khách vãng lai";
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

            var earlyCheckInMinutes = GetEarlyCheckInMinutes();
            var earliestStartUtc = bookingStartUtc.AddMinutes(-earlyCheckInMinutes);
            if (nowUtc < earliestStartUtc)
            {
                throw new BusinessRuleException(
                    $"Chưa đến giờ nhận bàn. Chỉ có thể nhận bàn trước giờ đặt tối đa {earlyCheckInMinutes} phút.");
            }
        }

        var tableId = request.TableId > 0
            ? request.TableId
            : booking?.TableId ?? throw new BusinessRuleException("A table is required to start a session.");
        var customerId = booking?.CustomerId ?? request.CustomerId;

        await EnsureCustomerCanStartSessionAsync(customerId, null, ct);
        await EnsureTableHasNoActiveSessionAsync(tableId, "Table already has an active session.", ct);

        var table = await db.VenueTables.FindAsync([tableId], ct) ?? throw new NotFoundException("Table not found.");
        if (!table.IsActive || (table.OperationalStatus != 1 && table.OperationalStatus != 3))
        {
            throw new BusinessRuleException("Table is not available for a new session.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        table.OperationalStatus = 2; // Occupied
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
            ?? throw BuildMissingPricingRuleException(table, session.StartedAtUtc, "thời điểm mở phiên");
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
            booking.Status = BookingStatuses.Completed;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await posNotificationService.NotifySessionStartedAsync((int)session.SessionId, (int)tableId, ct);
        if (booking is not null)
        {
            await posNotificationService.NotifyBookingUpdateAsync((int)booking.BookingId, ct);
        }

        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public Task<SessionDto> StartFromBookingAsync(long bookingId, long? tableId, long userId, CancellationToken ct) =>
        StartAsync(userId, new StartSessionRequest
        {
            BookingId = bookingId,
            TableId = tableId.GetValueOrDefault()
        }, ct);

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

    public async Task<SessionDto> CloseAsync(long sessionId, long? closedByUserId, CancellationToken ct)
    {
        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
        }

        var endedAtUtc = _clock.UtcNow;
        session.Status = 2; // Closed
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = closedByUserId;

        // Close all active assignments for this session and calculate their amounts
        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);

        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = endedAtUtc;
        }

        await CalculateSessionTimeChargeAsync(sessionId, endedAtUtc, true, ct);

        await db.SaveChangesAsync(ct);
        await posNotificationService.NotifySessionClosedAsync((int)session.SessionId, activeAssignments.FirstOrDefault()?.TableId is long tableId ? (int)tableId : null, ct);

        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
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

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            return await BuildCloseSessionResponseAsync(session, false, ct);
        }

        if (endedAtUtc <= session.StartedAtUtc)
        {
            throw new ValidationException("EndedAtUtc must be after session start time.");
        }

        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);

        if (activeAssignments.Count == 0)
        {
            throw new ConflictException("Session has no active table assignment to close.");
        }

        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = endedAtUtc;
        }

        session.Status = 2; // Closed
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = closedByUserId;

        var tableIds = activeAssignments.Select(x => x.TableId).Distinct().ToList();
        var tables = await db.VenueTables
            .Where(x => tableIds.Contains(x.TableId))
            .ToDictionaryAsync(x => x.TableId, ct);

        foreach (var assignment in activeAssignments)
        {
            if (!tables.TryGetValue(assignment.TableId, out var table))
            {
                throw new NotFoundException($"Table {assignment.TableId} not found.");
            }

            table.OperationalStatus = 1; // Available
        }

        var timeCharge = await CalculateSessionTimeChargeAsync(sessionId, endedAtUtc, true, ct);

        var totalDurationMinutes = timeCharge.ActualDurationMinutes;
        var timeSubtotal = timeCharge.SubtotalAmount;
        var productSubtotal = await db.Orders
            .Where(x => x.SessionId == sessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);

        EntityInvoice? invoice = null;
        if (request.GenerateInvoice)
        {
            invoice = await db.Invoices.FirstOrDefaultAsync(x => x.SessionId == sessionId && x.Status != 3, ct);

            if (invoice is null)
            {
                invoice = new EntityInvoice
                {
                    SessionId = sessionId,
                    CustomerId = session.CustomerId,
                    InvoiceCode = $"INV{now:yyyyMMddHHmmss}",
                    TimeSubtotalAmount = timeSubtotal,
                    ProductSubtotalAmount = productSubtotal,
                    SubtotalAmount = timeSubtotal + productSubtotal,
                    DiscountAmount = 0,
                    TaxAmount = 0,
                    GrandTotalAmount = timeSubtotal + productSubtotal,
                    PaidAmount = 0,
                    PaymentStatus = 1,
                    Status = 1,
                    IssuedByUserId = closedByUserId,
                    IssuedAtUtc = now
                };

                db.Invoices.Add(invoice);
            }
        }

        await db.SaveChangesAsync(ct);

        if (invoice is not null && !await db.InvoiceLines.AnyAsync(x => x.InvoiceId == invoice.InvoiceId, ct))
        {
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
                                    where order.SessionId == sessionId && order.Status != 3
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

            await db.SaveChangesAsync(ct);
        }

        if (invoice is not null)
        {
            await ApplyBookingDepositToInvoiceAsync(session, invoice, ct);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        await posNotificationService.NotifySessionClosedAsync((int)session.SessionId, activeAssignments.FirstOrDefault()?.TableId is long tableId ? (int)tableId : null, ct);

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

    public async Task<SessionDto> CancelAsync(long sessionId, long? cancelledByUserId, CancelSessionRequest request, CancellationToken ct)
    {
        request ??= new CancelSessionRequest();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

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

        var tableIds = activeAssignments.Select(x => x.TableId).Distinct().ToList();
        var tables = await db.VenueTables
            .Where(x => tableIds.Contains(x.TableId))
            .ToDictionaryAsync(x => x.TableId, ct);

        foreach (var assignment in activeAssignments)
        {
            assignment.EndedAtUtc = endedAtUtc;
            assignment.DurationMinutes = 0;
            assignment.Amount = 0;

            if (tables.TryGetValue(assignment.TableId, out var table))
            {
                table.OperationalStatus = 1; // Available
            }
        }

        session.Status = 3; // Cancelled
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = cancelledByUserId;
        session.Note = string.IsNullOrWhiteSpace(request.Reason) ? session.Note : request.Reason.Trim();

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

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
        var newTableId = request.TargetTableId;
        if (newTableId <= 0)
        {
            throw new ValidationException("ToTableId is required.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException(session.Status == 2
                ? "Session is closed. Reopen the session before transferring tables."
                : "Only active sessions can be transferred.");
        }

        var currentAssignment = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .OrderByDescending(x => x.StartedAtUtc)
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
        if (!newTable.IsActive || newTable.OperationalStatus != 1)
        {
            throw new BusinessRuleException("New table is not available.");
        }

        var endedAtUtc = _clock.UtcNow;
        await EnsureNoUpcomingBookingConflictAsync(newTableId, endedAtUtc, 15, ct);
        await EnsureSessionHasNoPaidInvoiceAsync(sessionId, ct);
        await CancelOpenInvoicesForSessionAsync(sessionId, "Invoice cancelled because the active session was transferred to another table.", ct);

        currentAssignment.EndedAtUtc = endedAtUtc;
        currentAssignment.DurationMinutes = GetDurationMinutes(currentAssignment.StartedAtUtc, endedAtUtc);
        currentAssignment.Amount = null;
        currentAssignment.Note = BuildTransferNote(request.Reason, request.Note);
        currentTable.OperationalStatus = request.MarkOldTableMaintenance ? 4 : 1;
        currentTable.UpdatedAtUtc = endedAtUtc;

        newTable.OperationalStatus = 2; // Occupied
        newTable.UpdatedAtUtc = endedAtUtc;

        var rule = await FindActiveRuleAsync(newTable.TableTypeId, endedAtUtc, ct)
            ?? throw BuildMissingPricingRuleException(newTable, endedAtUtc, "thời điểm chuyển bàn");
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
        await transaction.CommitAsync(ct);

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
            Message = "Chuyển bàn thành công"
        };
    }

    public async Task<SessionDetailDto> ReopenAsync(long sessionId, ReopenSessionRequest request, long? reopenedByUserId, CancellationToken ct)
    {
        request ??= new ReopenSessionRequest();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationException("Reason is required.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

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

        var table = await db.VenueTables.FindAsync([lastAssignment.TableId], ct) ?? throw new NotFoundException("Last table not found.");
        var now = _clock.UtcNow;

        if (request.ReopenLastTable)
        {
            if (!table.IsActive || table.OperationalStatus == 4)
            {
                throw new ConflictException("Last table is inactive or under maintenance and cannot be reopened.");
            }

            await EnsureTableHasNoActiveSessionAsync(table.TableId, "Last table already has another active session.", ct);
            await EnsureNoUpcomingBookingConflictAsync(table.TableId, now, 15, ct);

            lastAssignment.EndedAtUtc = null;
            lastAssignment.DurationMinutes = null;
            lastAssignment.Amount = null;
            lastAssignment.Note = BuildReopenNote(lastAssignment.Note, request.Reason);
            table.OperationalStatus = 2;
            table.UpdatedAtUtc = now;
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
        await transaction.CommitAsync(ct);

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
            var note = isBillable ? row.Assignment.Note : "Miễn tính do đổi bàn/bàn lỗi trong thời gian grace.";

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
                BillableDurationMinutes = isBillable ? actualMinutes : 0,
                HourlyRateSnapshot = row.Assignment.HourlyRateSnapshot > 0 ? row.Assignment.HourlyRateSnapshot : pricing.HourlyRate,
                HourlyRate = row.Assignment.HourlyRateSnapshot > 0 ? row.Assignment.HourlyRateSnapshot : pricing.HourlyRate,
                MinimumMinutes = pricing.MinimumMinutes,
                BillingBlockMinutes = pricing.BillingBlockMinutes,
                PricingPlanName = pricing.PricingPlanName,
                Amount = 0,
                IsCurrent = isCurrent,
                IsBillable = isBillable,
                Note = note
            });
        }

        var billableLines = lines.Where(x => x.IsBillable).ToList();
        var totalActualBillableMinutes = billableLines.Sum(x => x.ActualDurationMinutes);
        var totalActualMinutes = lines.Sum(x => x.ActualDurationMinutes);
        var ruleLine = billableLines.FirstOrDefault() ?? lines.FirstOrDefault();
        var minimumMinutes = ruleLine?.MinimumMinutes ?? 0;
        var billingBlockMinutes = ruleLine?.BillingBlockMinutes ?? 0;
        var sessionBillableMinutes = Math.Max(totalActualBillableMinutes, minimumMinutes);
        if (billingBlockMinutes > 0)
        {
            var remainder = sessionBillableMinutes % billingBlockMinutes;
            if (remainder > 0)
            {
                sessionBillableMinutes += billingBlockMinutes - remainder;
            }
        }

        var paddingMinutes = Math.Max(0, sessionBillableMinutes - totalActualBillableMinutes);
        var paddingTarget = billableLines.LastOrDefault(x => x.IsCurrent) ?? billableLines.LastOrDefault();
        if (paddingTarget is not null)
        {
            paddingTarget.BillableDurationMinutes += paddingMinutes;
            if (paddingMinutes > 0)
            {
                paddingTarget.Note = string.IsNullOrWhiteSpace(paddingTarget.Note)
                    ? "Phần làm tròn/minimum được cộng vào đoạn này."
                    : $"{paddingTarget.Note.Trim()} | Phần làm tròn/minimum được cộng vào đoạn này.";
            }
        }

        foreach (var line in lines)
        {
            line.Amount = line.IsBillable
                ? Math.Round(((decimal)line.BillableDurationMinutes / 60m) * line.HourlyRate, 2, MidpointRounding.AwayFromZero)
                : 0;

            if (!persist)
            {
                continue;
            }

            var assignment = rows.First(x => x.Assignment.SessionTableAssignmentId == line.SessionTableAssignmentId).Assignment;
            assignment.DurationMinutes = line.ActualDurationMinutes;
            assignment.Amount = line.Amount;
            assignment.HourlyRateSnapshot = line.HourlyRate;
            assignment.PricingPlanRuleId = line.PricingPlanRuleId;
            assignment.Note = line.Note;
        }

        return new SessionTimeChargeSummaryDto
        {
            ActualDurationMinutes = totalActualMinutes,
            BillableDurationMinutes = sessionBillableMinutes,
            MinimumMinutes = minimumMinutes,
            BillingBlockMinutes = billingBlockMinutes,
            SubtotalAmount = lines.Sum(x => x.Amount),
            Note = "Minimum/block áp dụng một lần cho toàn phiên, không áp lại sau mỗi lần chuyển bàn.",
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
            ?? throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thời điểm bắt đầu gán bàn");
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
        var message = $"Không tìm thấy bảng giá đang áp dụng cho bàn {table.TableCode} tại {context}.";
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
            throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thời điểm bắt đầu gán bàn");
        }
    }

    private async Task<PricingCharge> CalculatePreviewChargeAsync(SessionTableAssignment assignment, VenueTable table, int durationMinutes, CancellationToken ct)
    {
        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct);
        if (rule is null)
        {
            throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thời điểm bắt đầu gán bàn");
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
            ?? throw BuildMissingPricingRuleException(table, assignment.StartedAtUtc, "thời điểm mở phiên");

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
                        Description = $"Tích điểm từ hóa đơn {invoice.InvoiceCode} ({invoice.GrandTotalAmount:N0} VND)",
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
