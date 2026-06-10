using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Dashboard;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Services.Dashboard;

public class DashboardService(PoolHubDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(long? userId, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

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
                .Where(x => x.PaymentStatus == 2 && x.PaidAtUtc >= today && x.PaidAtUtc < tomorrow)
                .SumAsync(x => (decimal?)x.Amount, ct) ?? 0,
            LowStockProducts = await db.Products.CountAsync(x => x.StockQuantity <= 5, ct),
            UnreadNotifications = userId.HasValue
                ? await db.Notifications.CountAsync(x => !x.IsRead && x.UserId == userId.Value, ct)
                : 0
        };
    }
}
