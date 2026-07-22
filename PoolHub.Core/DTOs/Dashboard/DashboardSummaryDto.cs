namespace PoolHub.Core.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalTables { get; set; }
    public int AvailableTables { get; set; }
    public int InUseTables { get; set; }
    public int ReservedTables { get; set; }
    public int MaintenanceTables { get; set; }
    public int TodayBookings { get; set; }
    public int ActiveSessions { get; set; }
    public decimal TodayRevenue { get; set; }
    public int LowStockProducts { get; set; }
    public int UnreadNotifications { get; set; }
    public int PendingBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int UnpaidInvoices { get; set; }
    public int OrdersToday { get; set; }
    public int InvoicesToday { get; set; }
    public int SuccessfulPaymentsToday { get; set; }
    public int PendingPayments { get; set; }
    public int LongRunningSessions { get; set; }
    public int UpcomingBookings { get; set; }
    public int TodayAuditLogs { get; set; }
}
