using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class FloorDto
{
    public long FloorId { get; set; }

    [Required(ErrorMessage = "Floor Name is required.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Display Order must be non-negative.")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}
