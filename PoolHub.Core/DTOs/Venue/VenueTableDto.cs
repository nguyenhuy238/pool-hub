using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class VenueTableDto
{
    public long TableId { get; set; }

    public long ZoneId { get; set; }

    public long TableTypeId { get; set; }

    [Required(ErrorMessage = "Table Code is required.")]
    public string TableCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Table Name is required.")]
    public string TableName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
    public int Capacity { get; set; }

    public int OperationalStatus { get; set; }

    public bool IsActive { get; set; }
}
