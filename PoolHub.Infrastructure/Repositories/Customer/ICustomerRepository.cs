using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken ct);
}
