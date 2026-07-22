using PoolHub.Core.DTOs.Common;

namespace PoolHub.Core.DTOs.Session;

public class SessionQueryRequest : PaginationRequest
{
    public int? Status { get; set; }
    public long? TableId { get; set; }
    public DateTime? Date { get; set; }
}
