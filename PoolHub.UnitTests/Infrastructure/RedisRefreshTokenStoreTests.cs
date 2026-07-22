using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Services.Auth;

namespace PoolHub.UnitTests;

public class RedisRefreshTokenStoreTests
{
    [Fact]
    public async Task ValidateRefreshTokenAsync_WithIssuedToken_ReturnsValidRecord()
    {
        var store = CreateStore();

        var issued = await store.IssueRefreshTokenAsync(42, null, "127.0.0.1", "unit-test", default);
        var validation = await store.ValidateRefreshTokenAsync(issued.PlainValue, default);

        Assert.True(validation.IsValid);
        Assert.False(validation.IsReuseDetected);
        Assert.NotNull(validation.Record);
        Assert.Equal(42, validation.Record.UserId);
        Assert.NotEqual(issued.PlainValue.Split('.')[1], validation.Record.TokenHash);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_RevokesOldTokenAndAcceptsReplacement()
    {
        var store = CreateStore();

        var issued = await store.IssueRefreshTokenAsync(42, null, "127.0.0.1", "unit-test", default);
        var current = await store.ValidateRefreshTokenAsync(issued.PlainValue, default);
        var replacement = await store.RotateRefreshTokenAsync(current.Record!, "127.0.0.1", "unit-test", default);

        var oldTokenValidation = await store.ValidateRefreshTokenAsync(issued.PlainValue, default);
        var replacementValidation = await store.ValidateRefreshTokenAsync(replacement.PlainValue, default);

        Assert.False(oldTokenValidation.IsValid);
        Assert.True(oldTokenValidation.IsReuseDetected);
        Assert.True(replacementValidation.IsValid);
        Assert.Equal(current.Record!.FamilyId, replacementValidation.Record!.FamilyId);
    }

    private static RedisRefreshTokenStore CreateStore()
    {
        var cache = new TestDistributedCache();
        return new RedisRefreshTokenStore(cache, Options.Create(new AuthTokenOptions { RefreshTokenDays = 7 }));
    }

    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = [];

        public byte[]? Get(string key) => _values.GetValueOrDefault(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _values[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.CompletedTask;

        public void Remove(string key) =>
            _values.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
