using PoolHub.Core.Interfaces;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared;

namespace PoolHub.Services.Common;

public partial class CrudService : ICrudService
{
    protected readonly PoolHubDbContext db;

    public CrudService(PoolHubDbContext db)
    {
        this.db = db;
    }

    private static PagedResult<T> Page<T>(IReadOnlyCollection<T> items, int page, int size, int total) => new() { Items = items, PageNumber = page, PageSize = size, TotalCount = total };
}
