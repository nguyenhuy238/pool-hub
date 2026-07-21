using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Venue;

public class PricingRulePaginationRequest : PaginationRequest
{
    public long? PricingPlanId { get; set; }
    public long? TableTypeId { get; set; }
    public int? DayType { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}

public class PricingPlanPaginationRequest : PaginationRequest
{
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}
