namespace PoolHub.Core.DTOs.Dashboard;

public class AdminDashboardSummaryDto : DashboardSummaryDto
{
    public int ActiveTables { get; set; }
    public int TotalCustomers { get; set; }
}

public class RevenuePointDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class ActiveSessionDashboardDto
{
    public long SessionId { get; set; }
    public string SessionCode { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
}

public class LowStockProductDto
{
    public long ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
}

public class RecentAuditLogDto
{
    public long AuditLogId { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
