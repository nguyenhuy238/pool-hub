using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Dashboard;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Constants;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Dashboard;

public class DashboardService(PoolHubDbContext db, IClock? clock = null) : IDashboardService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task<DashboardSummaryDto> GetSummaryAsync(long? userId, CancellationToken ct)
    {
        var (today, tomorrow) = BusinessTime.LocalDateRangeToUtc(BusinessTime.UtcToVietnamLocalDate(_clock.UtcNow));

        return new DashboardSummaryDto
        {
            TotalTables = await db.VenueTables.CountAsync(ct),
            AvailableTables = await db.VenueTables.CountAsync(x => x.OperationalStatus == 1, ct),
            InUseTables = await db.VenueTables.CountAsync(x => x.OperationalStatus == 2, ct),
            ReservedTables = await db.VenueTables.CountAsync(x => x.OperationalStatus == 3, ct),
            MaintenanceTables = await db.VenueTables.CountAsync(x => x.OperationalStatus == 4, ct),
            TodayBookings = await db.Bookings.CountAsync(x => x.StartTimeUtc >= today && x.StartTimeUtc < tomorrow, ct),
            ActiveSessions = await db.Sessions.CountAsync(x => x.Status == 1, ct),
            TodayRevenue = await db.Payments
                .Where(x => x.PaymentStatus == PaymentStatuses.Completed && x.PaidAtUtc >= today && x.PaidAtUtc < tomorrow)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0,
            LowStockProducts = await db.Products.CountAsync(x => x.StockQuantity <= 5, ct),
            UnreadNotifications = userId.HasValue
                ? await db.Notifications.CountAsync(x => !x.IsRead && x.UserId == userId.Value, ct)
                : 0
        };
    }

    public async Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken ct)
    {
        var (today, tomorrow) = BusinessTime.LocalDateRangeToUtc(BusinessTime.UtcToVietnamLocalDate(_clock.UtcNow));
        var summary = await GetSummaryAsync(null, ct);

        return new AdminDashboardSummaryDto
        {
            TotalTables = summary.TotalTables,
            AvailableTables = summary.AvailableTables,
            InUseTables = summary.InUseTables,
            ReservedTables = summary.ReservedTables,
            MaintenanceTables = summary.MaintenanceTables,
            TodayBookings = summary.TodayBookings,
            ActiveSessions = summary.ActiveSessions,
            TodayRevenue = summary.TodayRevenue,
            LowStockProducts = summary.LowStockProducts,
            UnreadNotifications = await db.Notifications.CountAsync(x => !x.IsRead, ct),
            ActiveTables = await db.VenueTables.CountAsync(x => x.IsActive, ct),
            PendingBookings = await db.Bookings.CountAsync(x => x.Status == 1, ct),
            ConfirmedBookings = await db.Bookings.CountAsync(x => x.Status == 2, ct),
            UnpaidInvoices = await db.Invoices.CountAsync(x => x.PaymentStatus != InvoicePaymentStatuses.Paid, ct),
            TodayAuditLogs = await db.AuditLogs.CountAsync(x => x.CreatedAtUtc >= today && x.CreatedAtUtc < tomorrow, ct)
            ,OrdersToday = await db.Orders.CountAsync(x => x.CreatedAtUtc >= today && x.CreatedAtUtc < tomorrow, ct)
            ,TotalCustomers = await db.Customers.CountAsync(x => x.Status, ct)
            ,InvoicesToday = await db.Invoices.CountAsync(x => x.CreatedAtUtc >= today && x.CreatedAtUtc < tomorrow, ct)
        };
    }

    public async Task<List<RevenuePointDto>> GetRevenueAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var (from, to) = BusinessTime.LocalDateRangeToUtc(fromDate, toDate, _clock.UtcNow, 7);

        var paymentRows = await db.Payments
            .AsNoTracking()
            .Where(x => x.PaymentStatus == PaymentStatuses.Completed && x.PaidAtUtc >= from && x.PaidAtUtc < to)
            .ToListAsync(ct);

        var payments = paymentRows
            .Where(x => x.PaidAtUtc.HasValue)
            .GroupBy(x => BusinessTime.UtcToVietnamLocalDate(x.PaidAtUtc!.Value))
            .Select(x => new RevenuePointDto { Date = x.Key, Amount = x.Sum(p => p.Amount) })
            .ToList();

        var fromLocal = BusinessTime.UtcToVietnamLocalDate(from);
        var toLocal = BusinessTime.UtcToVietnamLocalDate(to.AddTicks(-1)).AddDays(1);
        return Enumerable.Range(0, Math.Max(1, (toLocal - fromLocal).Days))
            .Select(offset => fromLocal.AddDays(offset))
            .Select(date => new RevenuePointDto
            {
                Date = date,
                Amount = payments.FirstOrDefault(x => x.Date == date)?.Amount ?? 0
            })
            .ToList();
    }

    public async Task<List<ActiveSessionDashboardDto>> GetActiveSessionsAsync(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var sessions = await db.Sessions
            .AsNoTracking()
            .Where(x => x.Status == 1)
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(20)
            .Select(x => new { x.SessionId, x.SessionCode, x.StartedAtUtc })
            .ToListAsync(ct);

        return sessions.Select(x => new ActiveSessionDashboardDto
            {
                SessionId = x.SessionId,
                SessionCode = x.SessionCode,
                StartedAtUtc = x.StartedAtUtc,
                DurationMinutes = (int)Math.Max(0, Math.Ceiling((now - x.StartedAtUtc).TotalMinutes))
            })
            .ToList();
    }

    public Task<List<LowStockProductDto>> GetLowStockProductsAsync(CancellationToken ct) =>
        db.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.StockQuantity <= (x.LowStockThreshold ?? 5))
            .OrderBy(x => x.StockQuantity)
            .Take(20)
            .Select(x => new LowStockProductDto
            {
                ProductId = x.ProductId,
                Name = x.Name,
                Sku = x.Sku,
                StockQuantity = x.StockQuantity,
                LowStockThreshold = x.LowStockThreshold
            })
            .ToListAsync(ct);

    public Task<List<RecentAuditLogDto>> GetRecentAuditLogsAsync(CancellationToken ct) =>
        db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.AuditLogId)
            .Take(20)
            .Select(x => new RecentAuditLogDto
            {
                AuditLogId = x.AuditLogId,
                ActorUserId = x.ActorUserId,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
}
