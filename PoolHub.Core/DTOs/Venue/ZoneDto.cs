using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class ZoneDto
{
    public long ZoneId { get; set; }

    [Required(ErrorMessage = "Floor Id is required.")]
    public long FloorId { get; set; }

    [Required(ErrorMessage = "Zone Name is required.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Display Order must be non-negative.")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}
