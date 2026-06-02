namespace PoolHub.Core.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class Role : BaseEntity
{
    public long RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;
}

public class User : BaseEntity
{
    public long UserId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool Status { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public long? AssignedByUserId { get; set; }
}

public class RefreshToken : BaseEntity
{
    public long RefreshTokenId { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public bool IsRevoked { get; set; }
}

public class Customer : BaseEntity
{
    public long CustomerId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Note { get; set; }
    public bool Status { get; set; } = true;
}

public class Floor : BaseEntity
{
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Zone : BaseEntity
{
    public long ZoneId { get; set; }
    public long FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TableType : BaseEntity
{
    public long TableTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class VenueTable : BaseEntity
{
    public long TableId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ZoneId { get; set; }
    public long TableTypeId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public int OperationalStatus { get; set; } = 1;
}

public class PricingPlan : BaseEntity
{
    public long PricingPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAtUtc { get; set; }
}

public class PricingPlanRule : BaseEntity
{
    public long PricingPlanRuleId { get; set; }
    public long PricingPlanId { get; set; }
    public long TableTypeId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal HourlyRate { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Booking : BaseEntity
{
    public long BookingId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long CustomerId { get; set; }
    public long? TableId { get; set; }
    public long? TableTypeId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int NumberOfGuests { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
}

public class Session : BaseEntity
{
    public long SessionId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string SessionCode { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public long? BookingId { get; set; }
    public int Status { get; set; } = 1;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public long OpenedByUserId { get; set; }
    public long? ClosedByUserId { get; set; }
    public string? Note { get; set; }
}

public class SessionTableAssignment : BaseEntity
{
    public long SessionTableAssignmentId { get; set; }
    public long SessionId { get; set; }
    public long TableId { get; set; }
    public long? PricingPlanRuleId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int? DurationMinutes { get; set; }
    public decimal HourlyRateSnapshot { get; set; }
    public decimal? Amount { get; set; }
    public long? AssignedByUserId { get; set; }
    public string? Note { get; set; }
}

public class ProductCategory : BaseEntity
{
    public long ProductCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Product : BaseEntity
{
    public long ProductId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long ProductCategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int StockQuantity { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool IsStockTracked { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];
}

public class InventoryTransaction : BaseEntity
{
    public long InventoryTransactionId { get; set; }
    public long ProductId { get; set; }
    public int TransactionType { get; set; }
    public int Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Note { get; set; }
    public long? CreatedByUserId { get; set; }
}

public class Order : BaseEntity
{
    public long OrderId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string OrderCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long OrderedByUserId { get; set; }
    public int Status { get; set; } = 1;
    public decimal SubtotalAmount { get; set; }
    public string? Note { get; set; }
}

public class OrderItem : BaseEntity
{
    public long OrderItemId { get; set; }
    public long OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string? Note { get; set; }
}

public class Discount : BaseEntity
{
    public long DiscountId { get; set; }
    public string DiscountCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? MaxAmount { get; set; }
    public decimal? MinTimeSubtotal { get; set; }
    public string AppliesTo { get; set; } = "TIME";
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Invoice : BaseEntity
{
    public long InvoiceId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string InvoiceCode { get; set; } = string.Empty;
    public long SessionId { get; set; }
    public long? CustomerId { get; set; }
    public decimal TimeSubtotalAmount { get; set; }
    public decimal ProductSubtotalAmount { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int PaymentStatus { get; set; } = 1;
    public int Status { get; set; } = 1;
    public long? IssuedByUserId { get; set; }
    public DateTime? IssuedAtUtc { get; set; }
    public string? Note { get; set; }
}

public class InvoiceLine : BaseEntity
{
    public long InvoiceLineId { get; set; }
    public long InvoiceId { get; set; }
    public string LineType { get; set; } = string.Empty;
    public long? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotalAmount { get; set; }
}

public class InvoiceDiscount : BaseEntity
{
    public long InvoiceDiscountId { get; set; }
    public long InvoiceId { get; set; }
    public long DiscountId { get; set; }
    public long? AppliedByUserId { get; set; }
    public decimal AmountApplied { get; set; }
    public string? DescriptionSnapshot { get; set; }
}

public class PaymentMethod : BaseEntity
{
    public long PaymentMethodId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Payment : BaseEntity
{
    public long PaymentId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long InvoiceId { get; set; }
    public long PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public int PaymentStatus { get; set; } = 1;
    public string? TransactionCode { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public long? ReceivedByUserId { get; set; }
    public string? Note { get; set; }
}

public class Notification : BaseEntity
{
    public long NotificationId { get; set; }
    public long? UserId { get; set; }
    public long? CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public class AuditLog : BaseEntity
{
    public long AuditLogId { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public Guid? EntityPublicId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Description { get; set; }
}
