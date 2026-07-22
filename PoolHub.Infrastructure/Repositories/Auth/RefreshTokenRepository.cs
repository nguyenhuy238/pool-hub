using Microsoft.EntityFrameworkCore;
using PoolHub.Core.Entities;
using PoolHub.Infrastructure.Data;

namespace PoolHub.Infrastructure.Repositories;

public class RefreshTokenRepository(PoolHubDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindValidTokenAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsRevoked, ct);
}
