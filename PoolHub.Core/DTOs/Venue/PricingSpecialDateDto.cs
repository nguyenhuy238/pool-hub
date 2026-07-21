using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class PricingSpecialDateDto
{
    public long PricingSpecialDateId { get; set; }
    
    [Required]
    public DateTime Date { get; set; }
    
    [Range(3, 4)]
    public int DayType { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;
}
