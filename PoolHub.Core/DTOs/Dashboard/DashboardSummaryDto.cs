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
}
