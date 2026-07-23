using PoolHub.Shared;

namespace PoolHub.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken ct);
    Task<List<T>> GetAllAsync(CancellationToken ct);
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
