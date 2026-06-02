using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class FloorDto { public int FloorId { get; set; } [Required][StringLength(100)] public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class ZoneDto { public int ZoneId { get; set; } [Range(1, int.MaxValue)] public int FloorId { get; set; } [Required][StringLength(100)] public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }
public class TableTypeDto { public int TableTypeId { get; set; } [Required][StringLength(100)] public string Name { get; set; } = string.Empty; [Required][StringLength(50)] public string Code { get; set; } = string.Empty; [Range(1, 100)] public int DefaultCapacity { get; set; } }
public class VenueTableDto { public int TableId { get; set; } [Range(1, int.MaxValue)] public int ZoneId { get; set; } [Range(1, int.MaxValue)] public int TableTypeId { get; set; } [Required][StringLength(50)] public string TableCode { get; set; } = string.Empty; [Required][StringLength(100)] public string TableName { get; set; } = string.Empty; [Range(1, 100)] public int Capacity { get; set; } [Range(1, 5)] public int OperationalStatus { get; set; } public bool IsActive { get; set; } }
public class PricingPlanDto { public int PricingPlanId { get; set; } [Required][StringLength(100)] public string Name { get; set; } = string.Empty; public bool IsDefault { get; set; } public bool IsActive { get; set; } }
public class PricingPlanRuleDto { public int PricingPlanRuleId { get; set; } [Range(1, int.MaxValue)] public int PricingPlanId { get; set; } [Range(1, int.MaxValue)] public int TableTypeId { get; set; } [Range(0, 6)] public int DayOfWeek { get; set; } [Range(0, 1000000000)] public decimal HourlyRate { get; set; } }
public class SimpleUpsertRequest { [Required] public string Name { get; set; } = string.Empty; }
