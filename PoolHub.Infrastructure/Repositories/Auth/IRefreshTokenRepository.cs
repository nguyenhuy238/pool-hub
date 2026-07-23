using PoolHub.Core.Entities;

namespace PoolHub.Infrastructure.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindValidTokenAsync(string tokenHash, CancellationToken ct);
}
