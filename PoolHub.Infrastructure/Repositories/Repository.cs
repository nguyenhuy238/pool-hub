using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace PoolHub.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> GetByIdAsync(object id, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
}

public class Repository<T>(DbContext dbContext) : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();
    public IQueryable<T> Query() => _dbSet.AsQueryable();
    public async Task<T?> GetByIdAsync(object id, CancellationToken ct) => await _dbSet.FindAsync([id], ct);
    public Task AddAsync(T entity, CancellationToken ct) => _dbSet.AddAsync(entity, ct).AsTask();
    public void Update(T entity) => _dbSet.Update(entity);
    public void Remove(T entity) => _dbSet.Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct) => _dbSet.AnyAsync(predicate, ct);
}
