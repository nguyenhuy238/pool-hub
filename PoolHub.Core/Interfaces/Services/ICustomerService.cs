using PoolHub.Core.DTOs.Customer;
using PoolHub.Shared;

namespace PoolHub.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(CustomerQueryRequest request, CancellationToken ct);
    Task<CustomerDto> GetByIdAsync(long id, CancellationToken ct);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, long? actorUserId, CancellationToken ct);
    Task<CustomerDto> UpdateAsync(long id, UpdateCustomerRequest request, long? actorUserId, CancellationToken ct);
    Task UpdateStatusAsync(long id, bool status, long? actorUserId, CancellationToken ct);
}
