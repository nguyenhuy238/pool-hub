using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Shared.Exceptions;

namespace PoolHub.Services.Landing;

public class LandingPageSettingsService(PoolHubDbContext db) : ILandingPageSettingsService
{
    private const string LandingPageKey = "landing_page";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task<LandingPageSettingsDto> GetPublicLandingPageAsync(CancellationToken ct)
        => await GetOrCreateSettingsAsync(ct);

    public async Task<LandingPageSettingsDto> GetAdminSettingsAsync(CancellationToken ct)
        => await GetOrCreateSettingsAsync(ct);

    public async Task<PublicPricingSummaryDto> GetPricingSummaryAsync(CancellationToken ct)
    {
        var plans = await db.PricingPlans
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.PricingPlanId)
            .Select(x => new PricingPlanSummaryDto
            {
                PricingPlanId = x.PricingPlanId,
                Name = x.Name,
                IsDefault = x.IsDefault,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        var tableTypes = await db.TableTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new TableTypePricingSummaryDto
            {
                TableTypeId = x.TableTypeId,
                Name = x.Name,
                Code = x.Code,
                Description = x.Description,
                DefaultCapacity = x.DefaultCapacity,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        var activePlanIds = plans.Select(x => x.PricingPlanId).ToHashSet();
        var activeTableTypeIds = tableTypes.Select(x => x.TableTypeId).ToHashSet();
        var pricingRules = await db.PricingPlanRules
            .AsNoTracking()
            .Where(x => x.IsActive && activePlanIds.Contains(x.PricingPlanId) && activeTableTypeIds.Contains(x.TableTypeId))
            .OrderBy(x => x.TableTypeId)
            .ThenBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync(ct);

        var rules = pricingRules
            .Select(x => new PricingRuleSummaryDto
            {
                PricingPlanRuleId = x.PricingPlanRuleId,
                PricingPlanId = x.PricingPlanId,
                TableTypeId = x.TableTypeId,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime.ToString(@"hh\:mm\:ss"),
                EndTime = x.EndTime.ToString(@"hh\:mm\:ss"),
                HourlyRate = x.HourlyRate,
                MinimumMinutes = x.MinimumMinutes,
                BillingBlockMinutes = x.BillingBlockMinutes,
                IsActive = x.IsActive
            })
            .ToList();

        return new PublicPricingSummaryDto
        {
            Plans = plans,
            Rules = rules,
            TableTypes = tableTypes,
            FetchedAtUtc = DateTime.UtcNow
        };
    }

    public async Task<LandingPageSettingsDto> UpdateSettingsAsync(LandingPageSettingsDto dto, long currentUserId, CancellationToken ct)
    {
        ValidateSettings(dto);
        var setting = await GetOrCreateEntityAsync(ct);
        var oldJson = setting.SettingValueJson;
        var newJson = JsonSerializer.Serialize(dto, JsonOptions);

        setting.SettingValueJson = newJson;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        setting.UpdatedByUserId = currentUserId == 0 ? null : currentUserId;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId == 0 ? null : currentUserId,
            Action = "UPDATE_LANDING_PAGE_SETTINGS",
            EntityName = "SiteSettings",
            EntityId = setting.SiteSettingId,
            OldValues = oldJson,
            NewValues = newJson,
            Description = "Admin updated landing page settings"
        });

        await db.SaveChangesAsync(ct);
        return dto;
    }

    public async Task<LandingPageSettingsDto> ResetDefaultAsync(long currentUserId, CancellationToken ct)
    {
        var setting = await GetOrCreateEntityAsync(ct);
        var oldJson = setting.SettingValueJson;
        var defaults = CreateDefault();
        var newJson = JsonSerializer.Serialize(defaults, JsonOptions);

        setting.SettingValueJson = newJson;
        setting.UpdatedAtUtc = DateTime.UtcNow;
        setting.UpdatedByUserId = currentUserId == 0 ? null : currentUserId;

        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId == 0 ? null : currentUserId,
            Action = "RESET_LANDING_PAGE_SETTINGS",
            EntityName = "SiteSettings",
            EntityId = setting.SiteSettingId,
            OldValues = oldJson,
            NewValues = newJson,
            Description = "Admin reset landing page settings"
        });

        await db.SaveChangesAsync(ct);
        return defaults;
    }

    public void ValidateSettings(LandingPageSettingsDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.GeneralInfo.CenterName)) throw new ValidationException("Center name is required.");
        if (string.IsNullOrWhiteSpace(dto.Hero.Title)) throw new ValidationException("Hero title is required.");
        if (dto.BookingPolicy.MinDurationMinutes <= 0) throw new ValidationException("Minimum duration must be greater than zero.");
        if (dto.BookingPolicy.DefaultDurationMinutes <= 0) throw new ValidationException("Default duration must be greater than zero.");
        if (dto.BookingPolicy.HoldMinutes <= 0) throw new ValidationException("Hold minutes must be greater than zero.");
        if (dto.BookingPolicy.MaxDurationMinutes < dto.BookingPolicy.MinDurationMinutes) throw new ValidationException("Maximum duration must be greater than minimum duration.");
        if (dto.BookingPolicy.DefaultDurationMinutes < dto.BookingPolicy.MinDurationMinutes || dto.BookingPolicy.DefaultDurationMinutes > dto.BookingPolicy.MaxDurationMinutes) throw new ValidationException("Default duration must be between minimum and maximum duration.");
        if (dto.BookingPolicy.AdvanceBookingDays <= 0) throw new ValidationException("Advance booking days must be greater than zero.");
        ValidateHexColor(dto.Theme.PrimaryColor, nameof(dto.Theme.PrimaryColor));
        ValidateHexColor(dto.Theme.AccentColor, nameof(dto.Theme.AccentColor));
        if (string.IsNullOrWhiteSpace(dto.Hero.BackgroundImageUrl)) throw new ValidationException("Hero background image is required.");
        ValidateUrl(dto.About.ImageUrl, nameof(dto.About.ImageUrl), allowRelative: true);
        ValidateUrl(dto.QrCode.ImageUrl, nameof(dto.QrCode.ImageUrl), allowRelative: true);
        if (dto.Hero.UseVideo && string.IsNullOrWhiteSpace(dto.Hero.BackgroundVideoUrl)) throw new ValidationException("Hero video URL is required when video is enabled.");
        if (dto.Hero.UseVideo && string.IsNullOrWhiteSpace(dto.Hero.FallbackImageUrl)) throw new ValidationException("Hero fallback image is required when video is enabled.");
        if (dto.Services.Any(item => item.IsActive && string.IsNullOrWhiteSpace(item.ImageUrl))) throw new ValidationException("Active services require an image.");
        if (dto.Gallery.Any(item => item.IsActive && string.IsNullOrWhiteSpace(item.ImageUrl))) throw new ValidationException("Active gallery items require an image.");
        ValidateUrl(dto.Hero.BackgroundImageUrl, nameof(dto.Hero.BackgroundImageUrl), allowRelative: true);
        ValidateUrl(dto.Hero.FallbackImageUrl, nameof(dto.Hero.FallbackImageUrl), allowRelative: true);
        ValidateUrl(dto.Hero.BackgroundVideoUrl, nameof(dto.Hero.BackgroundVideoUrl), allowRelative: true);
        ValidateUrl(dto.GeneralInfo.GoogleMapsUrl, nameof(dto.GeneralInfo.GoogleMapsUrl), allowRelative: false);
        ValidateUrl(dto.GeneralInfo.LogoUrl, nameof(dto.GeneralInfo.LogoUrl), allowRelative: true);
        ValidateUrl(dto.GeneralInfo.FaviconUrl, nameof(dto.GeneralInfo.FaviconUrl), allowRelative: true);
        ValidateGoogleMaps(dto.GeneralInfo);
        ValidateUrl(dto.Seo.OgImageUrl, nameof(dto.Seo.OgImageUrl), allowRelative: true);
        ValidateMediaExtension(dto.GeneralInfo.LogoUrl, "Logo", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.GeneralInfo.FaviconUrl, "Favicon", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.Seo.OgImageUrl, "OG image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.Hero.BackgroundImageUrl, "Hero background image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.Hero.FallbackImageUrl, "Hero fallback image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.Hero.BackgroundVideoUrl, "Hero video", [".mp4", ".webm"]);
        ValidateMediaExtension(dto.About.ImageUrl, "About image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        ValidateMediaExtension(dto.QrCode.ImageUrl, "QR code", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        foreach (var item in dto.Gallery) ValidateUrl(item.ImageUrl, "Gallery image URL", allowRelative: true);
        foreach (var item in dto.Services) ValidateUrl(item.ImageUrl, "Service image URL", allowRelative: true);
        foreach (var item in dto.Gallery) ValidateMediaExtension(item.ImageUrl, "Gallery image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        foreach (var item in dto.Services) ValidateMediaExtension(item.ImageUrl, "Service image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        foreach (var item in dto.Reviews)
        {
            ValidateUrl(item.AvatarUrl, "Review avatar URL", allowRelative: true);
            ValidateUrl(item.CheckInImageUrl, "Review check-in image URL", allowRelative: true);
            ValidateMediaExtension(item.AvatarUrl, "Review avatar", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
            ValidateMediaExtension(item.CheckInImageUrl, "Review check-in image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
        }
        foreach (var item in dto.SocialLinks) ValidateSocialUrl(item);
        ValidateLink(dto.PromotionBanner.CtaLinkType, dto.PromotionBanner.CtaLink, "Promotion CTA");
        ValidateLink(dto.Hero.PrimaryCtaLinkType, dto.Hero.PrimaryCtaLink, "Hero primary CTA");
        ValidateLink(dto.Hero.SecondaryCtaLinkType, dto.Hero.SecondaryCtaLink, "Hero secondary CTA");
        foreach (var item in dto.Services.Where(item => !string.IsNullOrWhiteSpace(item.CtaText)))
        {
            ValidateLink(item.CtaLinkType, item.CtaLink ?? string.Empty, $"Service {item.Title} CTA");
        }
    }

    private async Task<LandingPageSettingsDto> GetOrCreateSettingsAsync(CancellationToken ct)
    {
        var setting = await GetOrCreateEntityAsync(ct);
        try
        {
            return JsonSerializer.Deserialize<LandingPageSettingsDto>(setting.SettingValueJson, JsonOptions) ?? CreateDefault();
        }
        catch (JsonException)
        {
            return CreateDefault();
        }
    }

    private async Task<SiteSetting> GetOrCreateEntityAsync(CancellationToken ct)
    {
        var setting = await db.SiteSettings.FirstOrDefaultAsync(x => x.SettingKey == LandingPageKey && x.IsActive, ct);
        if (setting is not null) return setting;

        setting = new SiteSetting
        {
            SettingKey = LandingPageKey,
            SettingValueJson = JsonSerializer.Serialize(CreateDefault(), JsonOptions),
            Description = "Public landing page configuration",
            IsActive = true
        };
        db.SiteSettings.Add(setting);
        await db.SaveChangesAsync(ct);
        return setting;
    }

    private static void ValidateUrl(string? value, string fieldName, bool allowRelative)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"{fieldName} cannot use an unsafe URL scheme.");
        }
        if (allowRelative && value.StartsWith('/')) return;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ValidationException($"{fieldName} must be a valid URL.");
        }
    }

    private static void ValidateHexColor(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || !System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$"))
        {
            throw new ValidationException($"{fieldName} must be a valid 6-digit hex color.");
        }
    }

    private static void ValidateMediaExtension(string? value, string fieldName, string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var path = value.Split('?', '#')[0];
        if (!allowedExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ValidationException($"{fieldName} has an unsupported file format.");
        }
    }

    private static void ValidateGoogleMaps(GeneralInfoDto info)
    {
        if (info.Latitude is < -90 or > 90) throw new ValidationException("Latitude must be between -90 and 90.");
        if (info.Longitude is < -180 or > 180) throw new ValidationException("Longitude must be between -180 and 180.");
        if (!string.IsNullOrWhiteSpace(info.GoogleMapsEmbedUrl) &&
            !info.GoogleMapsEmbedUrl.StartsWith("https://www.google.com/maps/embed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Google Maps embed URL must start with https://www.google.com/maps/embed.");
        }
        foreach (var value in new[] { info.GoogleMapsShareUrl, info.GoogleMapsDirectionUrl })
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!value.StartsWith("https://maps.google.com", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://www.google.com/maps", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Google Maps links must use maps.google.com or www.google.com/maps.");
            }
        }
    }

    private static void ValidateSocialUrl(SocialLinkDto item)
    {
        ValidateUrl(item.Url, $"{item.Platform} URL", allowRelative: false);
        if (!item.IsActive || string.IsNullOrWhiteSpace(item.Url)) return;
        var expectedHost = item.Platform.ToLowerInvariant() switch
        {
            "facebook" => "facebook.com",
            "tiktok" => "tiktok.com",
            "instagram" => "instagram.com",
            "youtube" => "youtube.com",
            "messenger" => "m.me",
            _ => null
        };
        if (expectedHost is not null && !new Uri(item.Url).Host.EndsWith(expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"{item.Platform} URL must use {expectedHost}.");
        }
    }

    private static void ValidateLink(string type, string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException($"{fieldName} link is required.");
        if (type == "external") ValidateUrl(value, fieldName, allowRelative: false);
        if (type == "phone" && !value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) throw new ValidationException($"{fieldName} phone link must start with tel:.");
        if (type == "email" && !value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) throw new ValidationException($"{fieldName} email link must start with mailto:.");
        if (type == "section" && !value.StartsWith('#')) throw new ValidationException($"{fieldName} section link must start with #.");
        if (type == "internal" && !value.StartsWith('/')) throw new ValidationException($"{fieldName} internal link must start with /.");
        if (type == "map" &&
            !value.StartsWith("https://maps.google.com", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://www.google.com/maps", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"{fieldName} map link must use Google Maps.");
        }
    }

    private static LandingPageSettingsDto CreateDefault() => new()
    {
        GeneralInfo = new GeneralInfoDto
        {
            CenterName = "PoolHub Center",
            Slogan = "Billiards & Entertainment",
            ShortDescription = "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại và đặt bàn online.",
            Hotline = "0901 234 567",
            Email = "hello@poolhub.vn",
            Address = "123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh",
            OpeningHours = "09:00 - 24:00 hằng ngày",
            GoogleMapsUrl = "https://www.google.com/maps",
            GoogleMapsShareUrl = "https://www.google.com/maps",
            GoogleMapsDirectionUrl = "https://www.google.com/maps",
            MapDisplayMode = "placeholder"
        },
        PromotionBanner = new PromotionBannerDto
        {
            IsEnabled = true,
            Content = "Tặng 30 phút chơi khi đặt bàn qua Web | Giảm 10% cho nhóm từ 4 người",
            CtaText = "Đặt ngay",
            CtaLink = "#booking",
            CtaLinkType = "section"
        },
        Hero = new HeroSectionDto
        {
            Subtitle = "PoolHub Billiards & Entertainment",
            Title = "Đặt bàn bi-a nhanh chóng - Trải nghiệm giải trí đẳng cấp",
            Description = "Không gian hiện đại, bàn chuẩn, đồ uống phục vụ tận bàn và đặt lịch online để nhóm bạn đến là có bàn chơi.",
            PrimaryCtaText = "Đặt bàn ngay",
            PrimaryCtaLink = "#booking",
            PrimaryCtaLinkType = "section",
            SecondaryCtaText = "Xem bảng giá",
            SecondaryCtaLink = "#pricing",
            SecondaryCtaLinkType = "section",
            BackgroundImageUrl = "/images/poolhub/hero.png",
            FallbackImageUrl = "/images/poolhub/hero.png",
            UseVideo = false,
            Badges = ["12+ bàn sẵn sàng", "09:00 mở cửa mỗi ngày", "4.8/5 đánh giá khách"]
        },
        About = new AboutSectionDto
        {
            Description = "PoolHub kết hợp bàn chơi chất lượng, dịch vụ tận bàn và hệ thống đặt lịch trực tuyến trong một không gian hiện đại.",
            ImageUrl = "/images/poolhub/hero.png"
        },
        UspItems =
        [
            new() { Title = "Bàn chuẩn thi đấu", Description = "Mặt bàn, bóng và cơ gậy được kiểm tra định kỳ.", DisplayOrder = 1 },
            new() { Title = "Không gian thoải mái", Description = "Điều hòa, wifi mạnh, khu vực chờ riêng.", DisplayOrder = 2 },
            new() { Title = "Phục vụ tận bàn", Description = "Đồ uống, đồ ăn nhẹ và combo nhóm được mang tới bàn nhanh.", DisplayOrder = 3 },
            new() { Title = "Đặt bàn online", Description = "Chọn ngày giờ, loại bàn và gửi yêu cầu trước.", DisplayOrder = 4 }
        ],
        Services =
        [
            new() { Title = "Billiard / Pool", Description = "Bàn pool phổ thông và bàn VIP.", ImageUrl = "/images/poolhub/hero.png", PriceText = "Từ 80.000đ/giờ", CtaText = "Đặt bàn", CtaLink = "#booking", CtaLinkType = "section", DisplayOrder = 1 },
            new() { Title = "Carom & Snooker", Description = "Khu vực yên tĩnh cho người chơi kỹ thuật.", ImageUrl = "/images/poolhub/hero.png", PriceText = "Từ 90.000đ/giờ", CtaText = "Đặt bàn", CtaLink = "#booking", CtaLinkType = "section", DisplayOrder = 2 },
            new() { Title = "Đồ uống & snack", Description = "Cà phê, trà trái cây, nước ngọt và combo nhóm.", ImageUrl = "/images/poolhub/hero.png", PriceText = "Từ 25.000đ", CtaText = "Xem thêm", CtaLink = "#contact", CtaLinkType = "section", DisplayOrder = 3 }
        ],
        PricingHighlights =
        [
            new() { Title = "Giờ vàng", Description = "Thứ 2 - Thứ 6 trước 17h", PriceText = "Từ 70.000đ/giờ", TimeRange = "09:00 - 17:00", Badge = "Giờ vàng", DisplayOrder = 1 },
            new() { Title = "Pool VIP", Description = "Không gian riêng cho nhóm", PriceText = "Từ 120.000đ/giờ", TableType = "VIP", Badge = "Best choice", DisplayOrder = 2 }
        ],
        Gallery =
        [
            new() { Title = "Khu bàn pool", ImageUrl = "/images/poolhub/hero.png", AltText = "Khu vực bàn pool hiện đại", DisplayOrder = 1, ImageType = "gallery" },
            new() { Title = "Quầy bar", ImageUrl = "/images/poolhub/hero.png", AltText = "Quầy bar PoolHub", DisplayOrder = 2, ImageType = "gallery" }
        ],
        Reviews =
        [
            new() { CustomerName = "Minh Quân", Rating = 5, Content = "Đặt bàn trên web nhanh, tới nơi là có bàn sẵn.", DisplayOrder = 1 },
            new() { CustomerName = "Thảo Vy", Rating = 5, Content = "Đồ uống lên nhanh và nhân viên xác nhận lịch rất gọn.", DisplayOrder = 2 },
            new() { CustomerName = "Hoàng Nam", Rating = 4, Content = "Bàn VIP chơi ổn, cơ gậy mới.", DisplayOrder = 3 }
        ],
        SocialLinks =
        [
            new() { Platform = "Facebook", Url = "https://www.facebook.com", Icon = "f", DisplayOrder = 1, IsActive = false },
            new() { Platform = "TikTok", Url = "https://www.tiktok.com", Icon = "t", DisplayOrder = 2, IsActive = false }
        ],
        BookingPolicy = new BookingPolicyDto(),
        Seo = new SeoSettingsDto(),
        Footer = new FooterSettingsDto(),
        Legal = new LegalSettingsDto(),
        Theme = new ThemeSettingsDto(),
        QrCode = new QrCodeSettingsDto()
    };
}
