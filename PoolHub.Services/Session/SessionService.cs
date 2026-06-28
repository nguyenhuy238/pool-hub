using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Session;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Exceptions;
using EntitySession = PoolHub.Core.Entities.Session;
using EntityInvoice = PoolHub.Core.Entities.Invoice;

namespace PoolHub.Services.Session;

public class SessionService(PoolHubDbContext db) : ISessionService
{
    public async Task<PagedResult<SessionDto>> GetSessionsAsync(SessionQueryRequest request, CancellationToken ct)
    {
        var query = db.Sessions.AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.Date.HasValue)
        {
            query = query.Where(x => x.StartedAtUtc.Date == request.Date.Value.Date);
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

        var now = DateTime.UtcNow;
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

        return await query
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
        var now = DateTime.UtcNow;

        var assignmentRows = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                                    join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                                    where assignment.SessionId == sessionId
                                    orderby assignment.StartedAtUtc
                                    select new { Assignment = assignment, Table = table })
            .ToListAsync(ct);

        var assignmentDtos = new List<SessionSummaryAssignmentDto>();
        foreach (var row in assignmentRows)
        {
            var isCurrent = row.Assignment.EndedAtUtc == null && session.Status == 1;
            var endedAtUtc = row.Assignment.EndedAtUtc ?? now;
            var durationMinutes = row.Assignment.DurationMinutes
                ?? Math.Max(0, (int)Math.Ceiling((endedAtUtc - row.Assignment.StartedAtUtc).TotalMinutes));
            var amount = row.Assignment.Amount
                ?? await CalculatePreviewAmountAsync(row.Assignment, row.Table, durationMinutes, ct);

            assignmentDtos.Add(new SessionSummaryAssignmentDto
            {
                SessionTableAssignmentId = row.Assignment.SessionTableAssignmentId,
                TableId = row.Assignment.TableId,
                TableCode = row.Table.TableCode,
                TableName = row.Table.TableName,
                StartedAtUtc = row.Assignment.StartedAtUtc,
                EndedAtUtc = row.Assignment.EndedAtUtc,
                DurationMinutes = durationMinutes,
                HourlyRateSnapshot = row.Assignment.HourlyRateSnapshot,
                Amount = amount,
                IsCurrent = isCurrent
            });
        }

        var currentAssignment = assignmentDtos.LastOrDefault(x => x.IsCurrent);
        var orderSubtotal = await db.Orders.AsNoTracking()
            .Where(x => x.SessionId == sessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);
        var timeSubtotal = assignmentDtos.Sum(x => x.Amount);

        return new SessionSummaryResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            CurrentDurationMinutes = assignmentDtos.Sum(x => x.DurationMinutes),
            TimeSubtotalAmount = timeSubtotal,
            OrderSubtotalAmount = orderSubtotal,
            SubtotalAmount = timeSubtotal + orderSubtotal,
            CurrentTable = currentAssignment is null
                ? null
                : new SessionSummaryTableDto
                {
                    TableId = currentAssignment.TableId,
                    TableCode = currentAssignment.TableCode,
                    TableName = currentAssignment.TableName
                },
            Assignments = assignmentDtos
        };
    }

    public async Task<SessionTimeChargesResponse> GetTimeChargesAsync(long sessionId, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(x => x.SessionId == sessionId, ct)
            ?? throw new NotFoundException("Session not found.");
        var now = DateTime.UtcNow;

        var rows = await (from assignment in db.SessionTableAssignments.AsNoTracking()
                          join table in db.VenueTables.AsNoTracking() on assignment.TableId equals table.TableId
                          where assignment.SessionId == sessionId
                          orderby assignment.StartedAtUtc
                          select new { Assignment = assignment, Table = table })
            .ToListAsync(ct);

        var items = new List<SessionTimeChargeItemDto>();
        foreach (var row in rows)
        {
            var endedAtUtc = row.Assignment.EndedAtUtc ?? now;
            var durationMinutes = row.Assignment.DurationMinutes
                ?? Math.Max(0, (int)Math.Ceiling((endedAtUtc - row.Assignment.StartedAtUtc).TotalMinutes));
            var charge = await CalculatePreviewChargeAsync(row.Assignment, row.Table, durationMinutes, ct);
            var amount = row.Assignment.Amount ?? charge.Amount;

            items.Add(new SessionTimeChargeItemDto
            {
                AssignmentId = row.Assignment.SessionTableAssignmentId,
                TableId = row.Assignment.TableId,
                TableCode = row.Table.TableCode,
                TableName = row.Table.TableName,
                PricingPlanRuleId = row.Assignment.PricingPlanRuleId ?? charge.PricingPlanRuleId,
                StartedAtUtc = row.Assignment.StartedAtUtc,
                EndedAtUtc = row.Assignment.EndedAtUtc,
                DurationMinutes = durationMinutes,
                BillableMinutes = charge.BillableMinutes,
                HourlyRateSnapshot = row.Assignment.HourlyRateSnapshot,
                Amount = amount
            });
        }

        return new SessionTimeChargesResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            TotalAmount = items.Sum(x => x.Amount),
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
            if (booking.Status != BookingStatuses.Confirmed)
            {
                throw new BusinessRuleException("Only confirmed bookings can start a session.");
            }

            if (await db.Sessions.AnyAsync(x => x.BookingId == booking.BookingId, ct))
            {
                throw new ConflictException("Booking already has a session.");
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
        var startedAtUtc = DateTime.UtcNow;

        var session = new EntitySession
        {
            SessionCode = $"SS{DateTime.UtcNow:yyyyMMddHHmmss}",
            BookingId = booking?.BookingId,
            CustomerId = customerId,
            OpenedByUserId = userId,
            StartedAtUtc = startedAtUtc,
            Status = 1
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        // Fetch pricing plan and active rule for the table at start time
        var rule = await FindActiveRuleAsync(table.TableTypeId, session.StartedAtUtc, ct);
        var hourlyRate = rule?.HourlyRate ?? 50000; // Default to 50,000 VND/hour if no rule is found

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

        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public Task<SessionDto> StartFromBookingAsync(long bookingId, long? tableId, long userId, CancellationToken ct) =>
        StartAsync(userId, new StartSessionRequest
        {
            BookingId = bookingId,
            TableId = tableId.GetValueOrDefault()
        }, ct);

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

        var endedAtUtc = DateTime.UtcNow;
        session.Status = 2; // Closed
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = closedByUserId;

        // Close all active assignments for this session and calculate their amounts
        var activeAssignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .ToListAsync(ct);

        foreach (var assignment in activeAssignments)
        {
            await CalculateAssignmentAmountAsync(assignment, endedAtUtc, ct);
        }

        await db.SaveChangesAsync(ct);
        return new SessionDto { SessionId = session.SessionId, SessionCode = session.SessionCode, StartedAtUtc = session.StartedAtUtc, EndedAtUtc = session.EndedAtUtc, Status = session.Status };
    }

    public async Task<CloseSessionResponse> CloseWithSummaryAsync(long sessionId, long? closedByUserId, CloseSessionRequest request, CancellationToken ct)
    {
        request ??= new CloseSessionRequest();

        var endedAtUtc = request.EndedAtUtc ?? DateTime.UtcNow;
        if (endedAtUtc > DateTime.UtcNow.AddMinutes(1))
        {
            throw new ValidationException("EndedAtUtc cannot be in the future.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status == 2)
        {
            throw new ConflictException("Session is already closed.");
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

            await CalculateAssignmentAmountStrictAsync(assignment, table, endedAtUtc, ct);
            table.OperationalStatus = 1; // Available
        }

        session.Status = 2; // Closed
        session.EndedAtUtc = endedAtUtc;
        session.ClosedByUserId = closedByUserId;

        var assignments = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId)
            .ToListAsync(ct);

        var totalDurationMinutes = assignments.Sum(x => x.DurationMinutes ?? 0);
        var timeSubtotal = assignments.Sum(x => x.Amount ?? 0);
        var productSubtotal = await db.Orders
            .Where(x => x.SessionId == sessionId && x.Status != 3)
            .SumAsync(x => x.SubtotalAmount, ct);

        EntityInvoice? invoice = null;
        if (request.GenerateInvoice)
        {
            var hasInvoice = await db.Invoices.AnyAsync(x => x.SessionId == sessionId, ct);
            if (hasInvoice)
            {
                throw new ConflictException("Invoice already exists for this session.");
            }

            invoice = new EntityInvoice
            {
                SessionId = sessionId,
                CustomerId = session.CustomerId,
                InvoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}",
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
                IssuedAtUtc = DateTime.UtcNow
            };

            db.Invoices.Add(invoice);
        }

        await db.SaveChangesAsync(ct);

        if (invoice is not null)
        {
            foreach (var assignment in assignments)
            {
                db.InvoiceLines.Add(new InvoiceLine
                {
                    InvoiceId = invoice.InvoiceId,
                    LineType = "TIME",
                    ReferenceId = assignment.SessionTableAssignmentId,
                    Description = $"Table time #{assignment.TableId}",
                    Quantity = (decimal)(assignment.DurationMinutes ?? 0) / 60m,
                    UnitPrice = assignment.HourlyRateSnapshot,
                    LineTotalAmount = assignment.Amount ?? 0
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

        await transaction.CommitAsync(ct);

        return new CloseSessionResponse
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            Status = session.Status,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = endedAtUtc,
            TotalDurationMinutes = totalDurationMinutes,
            TimeSubtotalAmount = timeSubtotal,
            InvoiceId = invoice?.InvoiceId,
            InvoiceCode = invoice?.InvoiceCode,
            InvoiceGenerated = invoice is not null
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

        var endedAtUtc = DateTime.UtcNow;
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

        return new SessionDto
        {
            SessionId = session.SessionId,
            SessionCode = session.SessionCode,
            StartedAtUtc = session.StartedAtUtc,
            EndedAtUtc = session.EndedAtUtc,
            Status = session.Status
        };
    }

    public async Task TransferTableAsync(long sessionId, long newTableId, long? assignedByUserId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var session = await db.Sessions.FindAsync([sessionId], ct) ?? throw new NotFoundException("Session not found.");
        if (session.Status != 1)
        {
            throw new BusinessRuleException("Only active sessions can be transferred.");
        }

        // 1. Validate if the new table is already occupied / has an active session
        var hasActiveSessionNewTable = await db.SessionTableAssignments
            .AnyAsync(sta => sta.TableId == newTableId && sta.EndedAtUtc == null && db.Sessions.Any(s => s.SessionId == sta.SessionId && s.Status == 1), ct);
        if (hasActiveSessionNewTable)
        {
            throw new ConflictException("New table already has an active session.");
        }

        // 2. Find new table
        var newTable = await db.VenueTables.FindAsync([newTableId], ct) ?? throw new NotFoundException("New table not found.");
        if (!newTable.IsActive || newTable.OperationalStatus != 1)
        {
            throw new BusinessRuleException("New table is not available.");
        }

        // 3. Find current active assignment
        var currentAssignment = await db.SessionTableAssignments
            .Where(x => x.SessionId == sessionId && x.EndedAtUtc == null)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(ct) ?? throw new NotFoundException("Active assignment not found.");

        var endedAtUtc = DateTime.UtcNow;

        // 4. Calculate amount for current assignment, set its EndedAtUtc and free the old table
        await CalculateAssignmentAmountAsync(currentAssignment, endedAtUtc, ct);

        // 5. Set new table status to 2 (Occupied)
        newTable.OperationalStatus = 2; // Occupied

        // 6. Create new assignment for the new table
        var rule = await FindActiveRuleAsync(newTable.TableTypeId, endedAtUtc, ct);
        var hourlyRate = rule?.HourlyRate ?? 50000;

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
        await transaction.CommitAsync(ct);
    }

    private async Task<PricingPlanRule?> FindActiveRuleAsync(long tableTypeId, DateTime time, CancellationToken ct)
    {
        int dayOfWeek = (int)time.DayOfWeek;
        TimeSpan timeOfDay = time.TimeOfDay;

        var activePlans = await db.PricingPlans
            .Where(p => p.IsActive && p.StartsAtUtc <= time && (p.EndsAtUtc == null || p.EndsAtUtc >= time))
            .ToListAsync(ct);

        if (!activePlans.Any()) return null;

        var planIds = activePlans.OrderBy(p => p.IsDefault).Select(p => p.PricingPlanId).ToList();

        foreach (var planId in planIds)
        {
            var rule = await db.PricingPlanRules
                .FirstOrDefaultAsync(r => r.PricingPlanId == planId && 
                                          r.TableTypeId == tableTypeId && 
                                          r.DayOfWeek == dayOfWeek && 
                                          r.StartTime <= timeOfDay && 
                                          r.EndTime >= timeOfDay && 
                                          r.IsActive, ct);
            if (rule != null) return rule;
        }

        var defaultPlan = activePlans.FirstOrDefault(p => p.IsDefault);
        if (defaultPlan != null)
        {
            var rule = await db.PricingPlanRules
                .FirstOrDefaultAsync(r => r.PricingPlanId == defaultPlan.PricingPlanId && 
                                          r.TableTypeId == tableTypeId && 
                                          r.DayOfWeek == dayOfWeek && 
                                          r.IsActive, ct);
            if (rule != null) return rule;
        }

        return null;
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
            decimal fallbackRate = 50000;
            assignment.HourlyRateSnapshot = fallbackRate;
            assignment.Amount = ((decimal)durationMinutes / 60m) * fallbackRate;
        }
    }

    private async Task<decimal> CalculatePreviewAmountAsync(SessionTableAssignment assignment, VenueTable table, int durationMinutes, CancellationToken ct)
    {
        var charge = await CalculatePreviewChargeAsync(assignment, table, durationMinutes, ct);
        return charge.Amount;
    }

    private async Task<(int BillableMinutes, decimal Amount, long? PricingPlanRuleId)> CalculatePreviewChargeAsync(SessionTableAssignment assignment, VenueTable table, int durationMinutes, CancellationToken ct)
    {
        var rule = await FindActiveRuleAsync(table.TableTypeId, assignment.StartedAtUtc, ct);
        if (rule is null)
        {
            var fallbackRate = assignment.HourlyRateSnapshot > 0 ? assignment.HourlyRateSnapshot : 50000m;
            return (durationMinutes, ((decimal)durationMinutes / 60m) * fallbackRate, assignment.PricingPlanRuleId);
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

        return (billableMinutes, ((decimal)billableMinutes / 60m) * rule.HourlyRate, rule.PricingPlanRuleId);
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
            ?? throw new ConflictException($"No active pricing rule found for table {table.TableCode} at session start time.");

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
}
