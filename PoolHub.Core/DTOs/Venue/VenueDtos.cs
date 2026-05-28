using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class FloorDto { public int FloorId { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class ZoneDto { public int ZoneId { get; set; } public int FloorId { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class TableTypeDto { public int TableTypeId { get; set; } public string Name { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public int DefaultCapacity { get; set; } }
public class VenueTableDto { public int TableId { get; set; } public int ZoneId { get; set; } public int TableTypeId { get; set; } public string TableCode { get; set; } = string.Empty; public string TableName { get; set; } = string.Empty; public int Capacity { get; set; } public int OperationalStatus { get; set; } }
public class PricingPlanDto { public int PricingPlanId { get; set; } public string Name { get; set; } = string.Empty; public bool IsDefault { get; set; } public bool IsActive { get; set; } }
public class PricingPlanRuleDto { public int PricingPlanRuleId { get; set; } public int PricingPlanId { get; set; } public int TableTypeId { get; set; } public int DayOfWeek { get; set; } public decimal HourlyRate { get; set; } }
public class SimpleUpsertRequest { [Required] public string Name { get; set; } = string.Empty; }
