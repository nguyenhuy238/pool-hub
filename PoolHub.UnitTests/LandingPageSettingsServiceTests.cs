using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Landing;
using PoolHub.Shared.Exceptions;

namespace PoolHub.UnitTests;

public class LandingPageSettingsServiceTests
{
    [Fact]
    public async Task UpdateSettingsAsync_WhenLegacyBadgeDelimiter_NormalizesAndCreatesAudit()
    {
        using var db = CreateDb();
        var service = new LandingPageSettingsService(db);
        var dto = ValidSettings();
        dto.Hero.Badges = ["12+ bàn | 4.8/5 đánh giá"];

        var result = await service.UpdateSettingsAsync(dto, 99, CancellationToken.None);

        Assert.Equal(["12+ bàn", "4.8/5 đánh giá"], result.Hero.Badges);
        var audit = await db.AuditLogs.SingleAsync(x => x.Action == "UPDATE_LANDING_PAGE_SETTINGS");
        Assert.Equal(99, audit.ActorUserId);
    }

    [Fact]
    public void ValidateSettings_WhenSectionTargetDoesNotExist_ThrowsValidationException()
    {
        var service = new LandingPageSettingsService(CreateDb());
        var dto = ValidSettings();
        dto.Hero.PrimaryCtaLink = "#availability";

        var exception = Assert.Throws<ValidationException>(() => service.ValidateSettings(dto));

        Assert.Contains("section target", exception.Message);
    }

    [Fact]
    public void ValidateSettings_WhenAddressHasOnlyNoise_ThrowsValidationException()
    {
        var service = new LandingPageSettingsService(CreateDb());
        var dto = ValidSettings();
        dto.GeneralInfo.Address = "11111";

        var exception = Assert.Throws<ValidationException>(() => service.ValidateSettings(dto));

        Assert.Contains("Address", exception.Message);
    }

    [Fact]
    public void ValidateSettings_WhenEmbeddedMapSelectedWithoutEmbedUrl_ThrowsValidationException()
    {
        var service = new LandingPageSettingsService(CreateDb());
        var dto = ValidSettings();
        dto.GeneralInfo.MapDisplayMode = "embed";
        dto.GeneralInfo.GoogleMapsEmbedUrl = "";

        var exception = Assert.Throws<ValidationException>(() => service.ValidateSettings(dto));

        Assert.Contains("embed URL", exception.Message);
    }

    private static PoolHubDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PoolHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PoolHubDbContext(options);
    }

    private static LandingPageSettingsDto ValidSettings() => new()
    {
        GeneralInfo = new GeneralInfoDto
        {
            CenterName = "PoolHub Center",
            Hotline = "0901234567",
            Email = "hello@poolhub.vn",
            Address = "123 Nguyen Trai, Quan 1",
            GoogleMapsUrl = "https://www.google.com/maps",
            GoogleMapsShareUrl = "https://www.google.com/maps",
            GoogleMapsDirectionUrl = "https://www.google.com/maps",
            MapDisplayMode = "placeholder"
        },
        Hero = new HeroSectionDto
        {
            Title = "Dat ban nhanh",
            BackgroundImageUrl = "/images/poolhub/hero.png",
            FallbackImageUrl = "/images/poolhub/hero.png",
            PrimaryCtaLink = "#booking",
            PrimaryCtaLinkType = "section",
            SecondaryCtaLink = "#pricing",
            SecondaryCtaLinkType = "section"
        },
        BookingPolicy = new BookingPolicyDto(),
        Theme = new ThemeSettingsDto(),
        PromotionBanner = new PromotionBannerDto
        {
            CtaLink = "#booking",
            CtaLinkType = "section"
        },
        DepositPayment = new DepositPaymentSettingsDto { IsDepositTransferEnabled = false }
    };
}
