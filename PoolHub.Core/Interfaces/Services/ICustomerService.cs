using PoolHub.Core.DTOs.Customer;
using PoolHub.Core.DTOs.Common;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

/// <summary>
/// Service quản lý thông tin khách hàng.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Lấy danh sách khách hàng có phân trang và tìm kiếm.
    /// </summary>
    /// <param name="request">Tham số query: search, status, pageNumber, pageSize.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerQueryRequest request, CancellationToken ct);

    /// <summary>
    /// Lấy thông tin chi tiết một khách hàng theo ID.
    /// Bao gồm tổng số lần đặt bàn.
    /// </summary>
    /// <param name="id">ID khách hàng.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CustomerDto> GetCustomerAsync(long id, CancellationToken ct);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct);

    /// <summary>
    /// Cập nhật thông tin khách hàng (fullName, email, note, status).
    /// Không cho phép thay đổi PhoneNumber qua endpoint này.
    /// </summary>
    /// <param name="id">ID khách hàng.</param>
    /// <param name="request">Dữ liệu cập nhật.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CustomerDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, long actorUserId, CancellationToken ct);
    Task UpdateStatusAsync(long id, bool status, long actorUserId, CancellationToken ct);
    Task<PagedResult<CustomerBookingHistoryDto>> GetBookingHistoryAsync(long id, PaginationRequest request, CancellationToken ct);
    Task<PagedResult<CustomerSessionHistoryDto>> GetSessionHistoryAsync(long id, PaginationRequest request, CancellationToken ct);
    Task<PagedResult<CustomerInvoiceHistoryDto>> GetInvoiceHistoryAsync(long id, PaginationRequest request, CancellationToken ct);
}
