namespace PoolHub.Core.DTOs.Common;

public class PaginationRequest { public int PageNumber { get; set; } = 1; public int PageSize { get; set; } = 20; public string? Search { get; set; } }
public class IdNameDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
