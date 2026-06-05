using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Venue;

public class SimpleUpsertRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
