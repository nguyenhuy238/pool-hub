using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Users;

public class UserQueryRequest : PaginationRequest
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public long? RoleId { get; set; }
}
