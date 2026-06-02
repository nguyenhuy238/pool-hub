using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class FloorDto { public long FloorId { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class ZoneDto { public long ZoneId { get; set; } public long FloorId { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class TableTypeDto { public long TableTypeId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public int DefaultCapacity { get; set; } }
public class VenueTableDto { public long TableId { get; set; } public long ZoneId { get; set; } public long TableTypeId { get; set; } public string TableCode { get; set; } = string.Empty; public string TableName { get; set; } = string.Empty; public int Capacity { get; set; } public int OperationalStatus { get; set; } }
public class PricingPlanDto { public long PricingPlanId { get; set; } public string Name { get; set; } = string.Empty; public bool IsDefault { get; set; } public bool IsActive { get; set; } }
public class PricingPlanRuleDto { public long PricingPlanRuleId { get; set; } public long PricingPlanId { get; set; } public long TableTypeId { get; set; } public int DayOfWeek { get; set; } public decimal HourlyRate { get; set; } }
public class SimpleUpsertRequest { [Required] public string Name { get; set; } = string.Empty; }
