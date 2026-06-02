namespace PoolHub.Core.Entities;

public class Floor : BaseEntity
{
    public int FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Zone : BaseEntity
{
    public int ZoneId { get; set; }
    public int FloorId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class TableType : BaseEntity
{
    public int TableTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; } = true;
}

public class VenueTable : BaseEntity
{
    public int TableId { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public int ZoneId { get; set; }
    public int TableTypeId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public int OperationalStatus { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class PricingPlan : BaseEntity
{
    public int PricingPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAtUtc { get; set; }
}

public class PricingPlanRule : BaseEntity
{
    public int PricingPlanRuleId { get; set; }
    public int PricingPlanId { get; set; }
    public int TableTypeId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal HourlyRate { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
