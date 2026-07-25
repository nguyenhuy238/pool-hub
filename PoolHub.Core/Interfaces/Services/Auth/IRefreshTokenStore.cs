using PoolHub.Core.DTOs.Auth;

namespace PoolHub.Core.Interfaces.Services;

public interface IRefreshTokenStore
{
    Task<RefreshTokenIssueResult> IssueRefreshTokenAsync(
        long userId,
        Guid? familyId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<RefreshTokenIssueResult> RotateRefreshTokenAsync(
        RefreshTokenRecord current,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task RevokeFamilyAsync(
        string familyId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task RevokeUserAsync(
        long userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
