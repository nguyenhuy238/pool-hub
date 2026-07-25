using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using PoolHub.Core.DTOs.Auth;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Auth;

public class RedisRefreshTokenStore(
    IDistributedCache cache,
    IOptions<AuthTokenOptions> authTokenOptions,
    IClock? clock = null) : IRefreshTokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AuthTokenOptions _authTokens = authTokenOptions.Value;
    private readonly IClock _clock = clock ?? SystemClock.Instance;

    public async Task<RefreshTokenIssueResult> IssueRefreshTokenAsync(
        long userId,
        Guid? familyId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var sessionId = Guid.NewGuid();
        var refreshTokenFamilyId = familyId ?? Guid.NewGuid();
        var secret = Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var plainValue = $"{sessionId:N}.{secret}";
        var expiresAtUtc = now.AddDays(Math.Max(1, _authTokens.RefreshTokenDays));
        var record = new RefreshTokenRecord
        {
            SessionId = sessionId.ToString("N"),
            UserId = userId,
            TokenHash = HashSecret(secret),
            FamilyId = refreshTokenFamilyId.ToString("N"),
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            CreatedByIp = ipAddress,
            CreatedByUserAgent = userAgent
        };

        await SaveRecordAsync(record, cancellationToken);
        await AddSessionToFamilyAsync(record.FamilyId, record.SessionId, expiresAtUtc, cancellationToken);
        await AddSessionToUserAsync(record.UserId, record.SessionId, expiresAtUtc, cancellationToken);
        return new RefreshTokenIssueResult { PlainValue = plainValue, Record = record };
    }

    public async Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (!TryParse(refreshToken, out var sessionId, out var secret))
            return new RefreshTokenValidationResult();

        var record = await GetRecordAsync(sessionId, cancellationToken);
        if (record is null || record.ExpiresAtUtc <= _clock.UtcNow)
            return new RefreshTokenValidationResult();

        if (record.RevokedAtUtc is not null)
            return new RefreshTokenValidationResult { IsReuseDetected = true, Record = record };

        var expectedHash = HashSecret(secret);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(record.TokenHash)))
        {
            return new RefreshTokenValidationResult { Record = record };
        }

        return new RefreshTokenValidationResult { IsValid = true, Record = record };
    }

    public async Task<RefreshTokenIssueResult> RotateRefreshTokenAsync(
        RefreshTokenRecord current,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var replacement = await IssueRefreshTokenAsync(
            current.UserId,
            Guid.ParseExact(current.FamilyId, "N"),
            ipAddress,
            userAgent,
            cancellationToken);

        current.RevokedAtUtc = _clock.UtcNow;
        current.RevokedByIp = ipAddress;
        current.ReplacedBySessionId = replacement.Record.SessionId;
        await SaveRecordAsync(current, cancellationToken);
        return replacement;
    }

    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (!TryParse(refreshToken, out var sessionId, out _))
            return;

        var record = await GetRecordAsync(sessionId, cancellationToken);
        if (record is null || record.RevokedAtUtc is not null)
            return;

        record.RevokedAtUtc = _clock.UtcNow;
        record.RevokedByIp = ipAddress;
        await SaveRecordAsync(record, cancellationToken);
    }

    public async Task RevokeFamilyAsync(
        string familyId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var sessionIds = await GetFamilySessionsAsync(familyId, cancellationToken);
        foreach (var sessionId in sessionIds)
        {
            var record = await GetRecordAsync(sessionId, cancellationToken);
            if (record is null || record.RevokedAtUtc is not null)
                continue;

            record.RevokedAtUtc = _clock.UtcNow;
            record.RevokedByIp = ipAddress;
            await SaveRecordAsync(record, cancellationToken);
        }
    }

    public async Task RevokeUserAsync(
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var sessionIds = await GetUserSessionsAsync(userId, cancellationToken);
        foreach (var sessionId in sessionIds)
        {
            var record = await GetRecordAsync(sessionId, cancellationToken);
            if (record is null || record.RevokedAtUtc is not null)
                continue;

            record.RevokedAtUtc = _clock.UtcNow;
            record.RevokedByIp = ipAddress;
            await SaveRecordAsync(record, cancellationToken);
        }
    }

    private async Task SaveRecordAsync(RefreshTokenRecord record, CancellationToken cancellationToken)
    {
        var ttl = record.ExpiresAtUtc - _clock.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return;

        await cache.SetStringAsync(
            BuildRefreshTokenKey(record.SessionId),
            JsonSerializer.Serialize(record, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    private async Task<RefreshTokenRecord?> GetRecordAsync(string sessionId, CancellationToken cancellationToken)
    {
        var json = await cache.GetStringAsync(BuildRefreshTokenKey(sessionId), cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<RefreshTokenRecord>(json, JsonOptions);
    }

    private async Task AddSessionToFamilyAsync(
        string familyId,
        string sessionId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var sessionIds = await GetFamilySessionsAsync(familyId, cancellationToken);
        if (!sessionIds.Contains(sessionId, StringComparer.Ordinal))
            sessionIds.Add(sessionId);

        var ttl = expiresAtUtc - _clock.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return;

        await cache.SetStringAsync(
            BuildFamilyKey(familyId),
            JsonSerializer.Serialize(sessionIds, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    private async Task AddSessionToUserAsync(
        long userId,
        string sessionId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var sessionIds = await GetUserSessionsAsync(userId, cancellationToken);
        if (!sessionIds.Contains(sessionId, StringComparer.Ordinal))
            sessionIds.Add(sessionId);

        var ttl = expiresAtUtc - _clock.UtcNow;
        if (ttl <= TimeSpan.Zero)
            return;

        await cache.SetStringAsync(
            BuildUserSessionsKey(userId),
            JsonSerializer.Serialize(sessionIds, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    private async Task<List<string>> GetFamilySessionsAsync(string familyId, CancellationToken cancellationToken)
    {
        var json = await cache.GetStringAsync(BuildFamilyKey(familyId), cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private async Task<List<string>> GetUserSessionsAsync(long userId, CancellationToken cancellationToken)
    {
        var json = await cache.GetStringAsync(BuildUserSessionsKey(userId), cancellationToken);
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static bool TryParse(string refreshToken, out string sessionId, out string secret)
    {
        sessionId = string.Empty;
        secret = string.Empty;
        var parts = refreshToken.Split('.', 2);
        if (parts.Length != 2 ||
            !Guid.TryParseExact(parts[0], "N", out var parsedSessionId) ||
            string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        sessionId = parsedSessionId.ToString("N");
        secret = parts[1];
        return true;
    }

    private static string HashSecret(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildRefreshTokenKey(string sessionId) =>
        $"poolhub:auth:refresh-token:{sessionId}";

    private static string BuildFamilyKey(string familyId) =>
        $"poolhub:auth:refresh-token-family:{familyId}";

    private static string BuildUserSessionsKey(long userId) =>
        $"poolhub:auth:refresh-token-user:{userId}";
}
