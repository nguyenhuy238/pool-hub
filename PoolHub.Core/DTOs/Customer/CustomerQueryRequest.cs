using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Customer;

public class CustomerQueryRequest : PaginationRequest
{
    public bool? Status { get; set; }
    public string? Phone { get; set; }
}
