using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PoolHub.Core.DTOs.Landing;
using PoolHub.Core.Entities;
using PoolHub.Core.Interfaces.Services;
using PoolHub.Infrastructure.Data;
using PoolHub.Services.Payments;
using PoolHub.Shared.Exceptions;
using PoolHub.Shared.Time;

namespace PoolHub.Services.Landing;

public class LandingPageSettingsService(PoolHubDbContext db, IClock? clock = null) : ILandingPageSettingsService
{
    private readonly IClock _clock = clock ?? SystemClock.Instance;
    private const string LandingPageKey = "landing_page";
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private static readonly HashSet<string> AllowedLinkTypes = ["section", "internal", "external", "phone", "map", "email"];
    private static readonly HashSet<string> PublicSectionTargets = ["#hero", "#about", "#services", "#pricing", "#booking", "#reviews", "#gallery", "#contact"];
    private static readonly HashSet<string> MapDisplayModes = ["embed", "placeholder", "external", "hidden"];

    public async Task<LandingPageSettingsDto> GetPublicLandingPageAsync(CancellationToken ct)
        => await GetOrCreateSettingsAsync(ct);

    public async Task<LandingPageSettingsDto> GetAdminSettingsAsync(CancellationToken ct)
        => await GetOrCreateSettingsAsync(ct);

    public async Task<DepositPaymentSettingsDto> GetDepositPaymentSettingsAsync(CancellationToken ct)
    {
        var methods = await db.PaymentMethods.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var config = methods
            .Select(BankTransferQrHelper.Parse)
            .FirstOrDefault(x => x is not null && (x.CanBuildDynamicQr || !string.IsNullOrWhiteSpace(x.QrImageUrl)));

        if (config is null)
        {
            return new DepositPaymentSettingsDto { IsDepositTransferEnabled = false };
        }

        return new DepositPaymentSettingsDto
        {
            IsDepositTransferEnabled = true,
            PaymentMethodCode = config.PaymentMethodCode,
            PaymentMethodName = config.PaymentMethodName,
            BankName = string.IsNullOrWhiteSpace(config.BankName) ? config.BankCode : config.BankName,
            BankCode = config.BankCode,
            BankAccountNumber = config.AccountNumber,
            BankAccountName = config.AccountName,
            DepositQrImageUrl = config.QrImageUrl,
            TransferContentTemplate = "POOLHUB {BookingCode} {PhoneNumber}"
        };
    }

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
            FetchedAtUtc = _clock.UtcNow
        };
    }

    public async Task<LandingPageSettingsDto> UpdateSettingsAsync(LandingPageSettingsDto dto, long currentUserId, CancellationToken ct)
    {
        dto = NormalizeSettings(dto);
        ValidateSettings(dto);
        var setting = await GetOrCreateEntityAsync(ct);
        var oldJson = setting.SettingValueJson;
        var newJson = JsonSerializer.Serialize(dto, JsonOptions);

        setting.SettingValueJson = newJson;
        setting.UpdatedAtUtc = _clock.UtcNow;
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
        setting.UpdatedAtUtc = _clock.UtcNow;
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
        if (!string.IsNullOrWhiteSpace(dto.GeneralInfo.Email) && !IsValidEmail(dto.GeneralInfo.Email)) throw new ValidationException("Email is invalid.");
        if (!string.IsNullOrWhiteSpace(dto.GeneralInfo.Hotline) && !IsValidPhone(dto.GeneralInfo.Hotline)) throw new ValidationException("Hotline is invalid.");
        if (!string.IsNullOrWhiteSpace(dto.GeneralInfo.Address) && !HasUsefulAddressText(dto.GeneralInfo.Address)) throw new ValidationException("Address must contain meaningful text.");
        if (!MapDisplayModes.Contains(dto.GeneralInfo.MapDisplayMode)) throw new ValidationException("Map display mode is invalid.");
        if (dto.PromotionBanner.StartAtUtc.HasValue && dto.PromotionBanner.EndAtUtc.HasValue &&
            dto.PromotionBanner.EndAtUtc.Value <= dto.PromotionBanner.StartAtUtc.Value)
        {
            throw new ValidationException("Promotion banner end date must be after start date.");
        }
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
        ValidateDepositPayment(dto.DepositPayment);
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
        if (dto.PromotionBanner.IsEnabled) ValidateLink(dto.PromotionBanner.CtaLinkType, dto.PromotionBanner.CtaLink, "Promotion CTA");
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
            return NormalizeSettings(JsonSerializer.Deserialize<LandingPageSettingsDto>(setting.SettingValueJson, JsonOptions) ?? CreateDefault());
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

    private static void ValidateDepositPayment(DepositPaymentSettingsDto settings)
    {
        if (!settings.IsDepositTransferEnabled) return;
        if (string.IsNullOrWhiteSpace(settings.BankName)) throw new ValidationException("Deposit bank name is required.");
        if (string.IsNullOrWhiteSpace(settings.BankAccountNumber)) throw new ValidationException("Deposit bank account number is required.");
        if (string.IsNullOrWhiteSpace(settings.BankAccountName)) throw new ValidationException("Deposit bank account name is required.");
        if (string.IsNullOrWhiteSpace(settings.TransferContentTemplate)) throw new ValidationException("Deposit transfer content template is required.");
        ValidateUrl(settings.DepositQrImageUrl, "Deposit QR image", allowRelative: true);
        ValidateMediaExtension(settings.DepositQrImageUrl, "Deposit QR image", [".jpg", ".jpeg", ".png", ".webp", ".gif", ".ico"]);
    }

    private static void ValidateGoogleMaps(GeneralInfoDto info)
    {
        if (info.Latitude is < -90 or > 90) throw new ValidationException("Latitude must be between -90 and 90.");
        if (info.Longitude is < -180 or > 180) throw new ValidationException("Longitude must be between -180 and 180.");
        if (info.MapDisplayMode == "embed" && string.IsNullOrWhiteSpace(info.GoogleMapsEmbedUrl))
        {
            throw new ValidationException("Google Maps embed URL is required when embedded map mode is selected.");
        }
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
        if (!AllowedLinkTypes.Contains(type)) throw new ValidationException($"{fieldName} link type is invalid.");
        if (string.IsNullOrWhiteSpace(value)) throw new ValidationException($"{fieldName} link is required.");
        if (type == "external") ValidateUrl(value, fieldName, allowRelative: false);
        if (type == "phone" && !value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) throw new ValidationException($"{fieldName} phone link must start with tel:.");
        if (type == "email" && !value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) throw new ValidationException($"{fieldName} email link must start with mailto:.");
        if (type == "section" && !PublicSectionTargets.Contains(value)) throw new ValidationException($"{fieldName} section target is not available on the public landing page.");
        if (type == "internal" && !value.StartsWith('/')) throw new ValidationException($"{fieldName} internal link must start with /.");
        if (type == "map" &&
            !value.StartsWith("https://maps.google.com", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://www.google.com/maps", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException($"{fieldName} map link must use Google Maps.");
        }
    }

    private static LandingPageSettingsDto NormalizeSettings(LandingPageSettingsDto? dto)
    {
        dto ??= CreateDefault();
        dto.SchemaVersion = CurrentSchemaVersion;
        dto.GeneralInfo ??= new GeneralInfoDto();
        dto.PromotionBanner ??= new PromotionBannerDto();
        dto.Hero ??= new HeroSectionDto();
        dto.About ??= new AboutSectionDto();
        dto.BookingPolicy ??= new BookingPolicyDto();
        dto.Seo ??= new SeoSettingsDto();
        dto.Footer ??= new FooterSettingsDto();
        dto.Legal ??= new LegalSettingsDto();
        dto.Theme ??= new ThemeSettingsDto();
        dto.QrCode ??= new QrCodeSettingsDto();
        dto.DepositPayment ??= new DepositPaymentSettingsDto();
        dto.UspItems ??= [];
        dto.Services ??= [];
        dto.PricingHighlights ??= [];
        dto.Gallery ??= [];
        dto.Reviews ??= [];
        dto.SocialLinks ??= [];

        dto.GeneralInfo.CenterName = Clean(dto.GeneralInfo.CenterName);
        dto.GeneralInfo.Slogan = Clean(dto.GeneralInfo.Slogan);
        dto.GeneralInfo.ShortDescription = Clean(dto.GeneralInfo.ShortDescription);
        dto.GeneralInfo.Hotline = Clean(dto.GeneralInfo.Hotline);
        dto.GeneralInfo.Email = Clean(dto.GeneralInfo.Email);
        dto.GeneralInfo.Address = Clean(dto.GeneralInfo.Address);
        dto.GeneralInfo.OpeningHours = Clean(dto.GeneralInfo.OpeningHours);
        dto.GeneralInfo.FacebookUrl = NullIfWhiteSpace(dto.GeneralInfo.FacebookUrl);
        dto.GeneralInfo.TikTokUrl = NullIfWhiteSpace(dto.GeneralInfo.TikTokUrl);
        dto.GeneralInfo.ZaloUrl = NullIfWhiteSpace(dto.GeneralInfo.ZaloUrl);
        dto.GeneralInfo.GoogleMapsUrl = NullIfWhiteSpace(dto.GeneralInfo.GoogleMapsUrl);
        dto.GeneralInfo.GoogleMapsEmbedUrl = NullIfWhiteSpace(dto.GeneralInfo.GoogleMapsEmbedUrl);
        dto.GeneralInfo.GoogleMapsShareUrl = NullIfWhiteSpace(dto.GeneralInfo.GoogleMapsShareUrl);
        dto.GeneralInfo.GoogleMapsDirectionUrl = NullIfWhiteSpace(dto.GeneralInfo.GoogleMapsDirectionUrl);
        dto.GeneralInfo.PlaceId = NullIfWhiteSpace(dto.GeneralInfo.PlaceId);
        dto.GeneralInfo.LogoUrl = NullIfWhiteSpace(dto.GeneralInfo.LogoUrl);
        dto.GeneralInfo.FaviconUrl = NullIfWhiteSpace(dto.GeneralInfo.FaviconUrl);
        dto.GeneralInfo.MapDisplayMode = Clean(dto.GeneralInfo.MapDisplayMode).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(dto.GeneralInfo.MapDisplayMode)) dto.GeneralInfo.MapDisplayMode = "placeholder";

        dto.PromotionBanner.Content = Clean(dto.PromotionBanner.Content);
        dto.PromotionBanner.CtaText = Clean(dto.PromotionBanner.CtaText);
        dto.PromotionBanner.CtaLink = Clean(dto.PromotionBanner.CtaLink);
        dto.PromotionBanner.CtaLinkType = Clean(dto.PromotionBanner.CtaLinkType).ToLowerInvariant();
        dto.Hero.Subtitle = Clean(dto.Hero.Subtitle);
        dto.Hero.Title = Clean(dto.Hero.Title);
        dto.Hero.Description = Clean(dto.Hero.Description);
        dto.Hero.PrimaryCtaText = Clean(dto.Hero.PrimaryCtaText);
        dto.Hero.PrimaryCtaLink = Clean(dto.Hero.PrimaryCtaLink);
        dto.Hero.PrimaryCtaLinkType = Clean(dto.Hero.PrimaryCtaLinkType).ToLowerInvariant();
        dto.Hero.SecondaryCtaText = Clean(dto.Hero.SecondaryCtaText);
        dto.Hero.SecondaryCtaLink = Clean(dto.Hero.SecondaryCtaLink);
        dto.Hero.SecondaryCtaLinkType = Clean(dto.Hero.SecondaryCtaLinkType).ToLowerInvariant();
        dto.Hero.BackgroundImageUrl = Clean(dto.Hero.BackgroundImageUrl);
        dto.Hero.BackgroundVideoUrl = NullIfWhiteSpace(dto.Hero.BackgroundVideoUrl);
        dto.Hero.FallbackImageUrl = Clean(dto.Hero.FallbackImageUrl);
        dto.Hero.Badges = dto.Hero.Badges
            .SelectMany(x => (x ?? string.Empty).Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(Clean)
            .Where(x => x.Length > 0)
            .Take(6)
            .ToList();

        dto.About.Eyebrow = Clean(dto.About.Eyebrow);
        dto.About.Title = Clean(dto.About.Title);
        dto.About.Description = Clean(dto.About.Description);
        dto.About.ImageUrl = NullIfWhiteSpace(dto.About.ImageUrl);
        NormalizeOrdered(dto.UspItems);
        NormalizeOrdered(dto.Services);
        NormalizeOrdered(dto.PricingHighlights);
        NormalizeOrdered(dto.Gallery);

        foreach (var item in dto.Services)
        {
            item.PriceText = NullIfWhiteSpace(item.PriceText);
            item.CtaText = NullIfWhiteSpace(item.CtaText);
            item.CtaLink = NullIfWhiteSpace(item.CtaLink);
            item.CtaLinkType = Clean(item.CtaLinkType).ToLowerInvariant();
        }

        foreach (var item in dto.Gallery)
        {
            item.ImageUrl = Clean(item.ImageUrl);
            item.AltText = Clean(item.AltText);
            item.Caption = NullIfWhiteSpace(item.Caption);
            item.ImageType = Clean(item.ImageType);
        }

        foreach (var item in dto.Reviews)
        {
            item.CustomerName = Clean(item.CustomerName);
            item.AvatarUrl = NullIfWhiteSpace(item.AvatarUrl);
            item.Content = Clean(item.Content);
            item.CheckInImageUrl = NullIfWhiteSpace(item.CheckInImageUrl);
        }

        foreach (var item in dto.SocialLinks)
        {
            item.Platform = Clean(item.Platform);
            item.Url = Clean(item.Url);
            item.Icon = Clean(item.Icon);
        }

        dto.BookingPolicy.SuccessMessage = Clean(dto.BookingPolicy.SuccessMessage);
        dto.BookingPolicy.PolicyNote = Clean(dto.BookingPolicy.PolicyNote);
        dto.Seo.MetaTitle = Clean(dto.Seo.MetaTitle);
        dto.Seo.MetaDescription = Clean(dto.Seo.MetaDescription);
        dto.Seo.MetaKeywords = NullIfWhiteSpace(dto.Seo.MetaKeywords);
        dto.Seo.OgImageUrl = NullIfWhiteSpace(dto.Seo.OgImageUrl);
        dto.Seo.CanonicalUrl = NullIfWhiteSpace(dto.Seo.CanonicalUrl);
        dto.Footer.MenuTitle = Clean(dto.Footer.MenuTitle);
        dto.Footer.PolicyTitle = Clean(dto.Footer.PolicyTitle);
        dto.Footer.Copyright = Clean(dto.Footer.Copyright);
        dto.Legal.PrivacyPolicy = Clean(dto.Legal.PrivacyPolicy);
        dto.Legal.TermsOfService = Clean(dto.Legal.TermsOfService);
        dto.Theme.PrimaryColor = Clean(dto.Theme.PrimaryColor);
        dto.Theme.AccentColor = Clean(dto.Theme.AccentColor);
        dto.QrCode.ImageUrl = NullIfWhiteSpace(dto.QrCode.ImageUrl);
        dto.QrCode.Caption = NullIfWhiteSpace(dto.QrCode.Caption);
        dto.DepositPayment.PaymentMethodCode = Clean(dto.DepositPayment.PaymentMethodCode);
        dto.DepositPayment.PaymentMethodName = Clean(dto.DepositPayment.PaymentMethodName);
        dto.DepositPayment.BankName = Clean(dto.DepositPayment.BankName);
        dto.DepositPayment.BankCode = Clean(dto.DepositPayment.BankCode);
        dto.DepositPayment.BankAccountNumber = Clean(dto.DepositPayment.BankAccountNumber);
        dto.DepositPayment.BankAccountName = Clean(dto.DepositPayment.BankAccountName);
        dto.DepositPayment.DepositQrImageUrl = NullIfWhiteSpace(dto.DepositPayment.DepositQrImageUrl);
        dto.DepositPayment.TransferContentTemplate = Clean(dto.DepositPayment.TransferContentTemplate);

        return dto;
    }

    private static void NormalizeOrdered<T>(List<T> items) where T : OrderedLandingItemDto
    {
        for (var i = 0; i < items.Count; i++)
        {
            items[i].Title = Clean(items[i].Title);
            items[i].Description = Clean(items[i].Description);
            items[i].ImageUrl = NullIfWhiteSpace(items[i].ImageUrl);
            if (items[i].DisplayOrder <= 0) items[i].DisplayOrder = i + 1;
        }
    }

    private static string Clean(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value.Trim(), @"\s+", " ");
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : Clean(value);
    private static bool IsValidEmail(string value)
    {
        try { return new MailAddress(value).Address == value; }
        catch { return false; }
    }
    private static bool IsValidPhone(string value)
    {
        var digits = value.Count(char.IsDigit);
        return digits is >= 8 and <= 15 && Regex.IsMatch(value, @"^[0-9+()\s.-]+$");
    }
    private static bool HasUsefulAddressText(string value) => value.Trim().Length >= 8 && value.Any(char.IsLetter);

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
        QrCode = new QrCodeSettingsDto(),
        DepositPayment = new DepositPaymentSettingsDto
        {
            BankName = "MB Bank",
            BankAccountNumber = "989420048989",
            BankAccountName = "POOLHUB",
            DepositQrImageUrl = "/images/poolhub/hero.png",
            TransferContentTemplate = "POOLHUB {BookingCode} {PhoneNumber}",
            IsDepositTransferEnabled = true
        }
    };
}
