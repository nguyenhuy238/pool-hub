using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class TableTypeDto
{
    public long TableTypeId { get; set; }

    [Required(ErrorMessage = "Table Type Name is required.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Table Type Code is required.")]
    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Default Capacity must be at least 1.")]
    public int DefaultCapacity { get; set; }

    public bool IsActive { get; set; }
}
