using PoolHub.Core.DTOs.Landing;

namespace PoolHub.Core.Interfaces.Services;

public interface ILandingPageSettingsService
{
    Task<LandingPageSettingsDto> GetPublicLandingPageAsync(CancellationToken ct);
    Task<LandingPageSettingsDto> GetAdminSettingsAsync(CancellationToken ct);
    Task<DepositPaymentSettingsDto> GetDepositPaymentSettingsAsync(CancellationToken ct);
    Task<PublicPricingSummaryDto> GetPricingSummaryAsync(CancellationToken ct);
    Task<LandingPageSettingsDto> UpdateSettingsAsync(LandingPageSettingsDto dto, long currentUserId, CancellationToken ct);
    Task<LandingPageSettingsDto> ResetDefaultAsync(long currentUserId, CancellationToken ct);
    void ValidateSettings(LandingPageSettingsDto dto);
}
