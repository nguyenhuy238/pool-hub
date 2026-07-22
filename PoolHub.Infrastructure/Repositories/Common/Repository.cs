using Microsoft.EntityFrameworkCore;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;

namespace PoolHub.Infrastructure.Repositories;

public class Repository<T>(PoolHubDbContext dbContext) : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();
    public async Task<T?> GetByIdAsync(object id, CancellationToken ct) => await _dbSet.FindAsync([id], ct);
    public Task<List<T>> GetAllAsync(CancellationToken ct) => _dbSet.ToListAsync(ct);
    public async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        var total = await _dbSet.CountAsync(ct);
        var items = await _dbSet.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T> { Items = items, PageNumber = pageNumber, PageSize = pageSize, TotalCount = total };
    }

    public Task AddAsync(T entity, CancellationToken ct) => _dbSet.AddAsync(entity, ct).AsTask();
    public void Update(T entity) => _dbSet.Update(entity);
    public void Delete(T entity) => _dbSet.Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
