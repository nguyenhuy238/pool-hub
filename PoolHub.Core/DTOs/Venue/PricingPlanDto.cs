using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class PricingPlanDto
{
    public long PricingPlanId { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
