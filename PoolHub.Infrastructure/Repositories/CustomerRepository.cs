using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken ct);
}

public class CustomerRepository(PoolHubDbContext db) : ICustomerRepository
{
    public Task<Customer?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken ct) =>
        db.Customers.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, ct);
}
