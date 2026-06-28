using System.ComponentModel.DataAnnotations;

namespace PoolHub.Core.DTOs.Landing;

public class LandingPageSettingsDto
{
    public GeneralInfoDto GeneralInfo { get; set; } = new();
    public PromotionBannerDto PromotionBanner { get; set; } = new();
    public HeroSectionDto Hero { get; set; } = new();
    public AboutSectionDto About { get; set; } = new();
    public List<UspItemDto> UspItems { get; set; } = [];
    public List<ServiceHighlightDto> Services { get; set; } = [];
    public List<PricingHighlightDto> PricingHighlights { get; set; } = [];
    public List<GalleryItemDto> Gallery { get; set; } = [];
    public List<ReviewDto> Reviews { get; set; } = [];
    public List<SocialLinkDto> SocialLinks { get; set; } = [];
    public BookingPolicyDto BookingPolicy { get; set; } = new();
    public SeoSettingsDto Seo { get; set; } = new();
    public FooterSettingsDto Footer { get; set; } = new();
    public LegalSettingsDto Legal { get; set; } = new();
    public ThemeSettingsDto Theme { get; set; } = new();
    public QrCodeSettingsDto QrCode { get; set; } = new();
}

public class PublicPricingSummaryDto
{
    public List<PricingPlanSummaryDto> Plans { get; set; } = [];
    public List<PricingRuleSummaryDto> Rules { get; set; } = [];
    public List<TableTypePricingSummaryDto> TableTypes { get; set; } = [];
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PricingPlanSummaryDto
{
    public long PricingPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class PricingRuleSummaryDto
{
    public long PricingPlanRuleId { get; set; }
    public long PricingPlanId { get; set; }
    public long TableTypeId { get; set; }
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public int MinimumMinutes { get; set; }
    public int BillingBlockMinutes { get; set; }
    public bool IsActive { get; set; }
}

public class TableTypePricingSummaryDto
{
    public long TableTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultCapacity { get; set; }
    public bool IsActive { get; set; }
}

public class AboutSectionDto
{
    public bool IsEnabled { get; set; } = true;
    public string Eyebrow { get; set; } = "Về PoolHub";
    public string Title { get; set; } = "Không gian giải trí dành cho mọi cuộc gặp";
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class FooterSettingsDto
{
    public string MenuTitle { get; set; } = "Menu nhanh";
    public string PolicyTitle { get; set; } = "Chính sách";
    public string Copyright { get; set; } = "Copyright 2026 PoolHub.";
}

public class LegalSettingsDto
{
    public string PrivacyPolicy { get; set; } = string.Empty;
    public string TermsOfService { get; set; } = string.Empty;
}

public class ThemeSettingsDto
{
    public string PrimaryColor { get; set; } = "#0f5d4b";
    public string AccentColor { get; set; } = "#c89d3f";
}

public class QrCodeSettingsDto
{
    public bool IsEnabled { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
}

public class GeneralInfoDto
{
    [Required] public string CenterName { get; set; } = "PoolHub Center";
    public string Slogan { get; set; } = "Billiards & Entertainment";
    public string ShortDescription { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string? FacebookUrl { get; set; }
    public string? TikTokUrl { get; set; }
    public string? ZaloUrl { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? GoogleMapsEmbedUrl { get; set; }
    public string? GoogleMapsShareUrl { get; set; }
    public string? GoogleMapsDirectionUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PlaceId { get; set; }
    public string MapDisplayMode { get; set; } = "placeholder";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
}

public class PromotionBannerDto
{
    public bool IsEnabled { get; set; } = true;
    public string Content { get; set; } = "Tặng 30 phút chơi khi đặt bàn qua Web";
    public string CtaText { get; set; } = "Đặt ngay";
    public string CtaLink { get; set; } = "#booking";
    public string CtaLinkType { get; set; } = "section";
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
}

public class HeroSectionDto
{
    public string Subtitle { get; set; } = "PoolHub Billiards & Entertainment";
    [Required] public string Title { get; set; } = "Đặt bàn bi-a nhanh chóng - Trải nghiệm giải trí đẳng cấp";
    public string Description { get; set; } = string.Empty;
    public string PrimaryCtaText { get; set; } = "Đặt bàn ngay";
    public string PrimaryCtaLink { get; set; } = "#booking";
    public string PrimaryCtaLinkType { get; set; } = "section";
    public string SecondaryCtaText { get; set; } = "Xem bảng giá";
    public string SecondaryCtaLink { get; set; } = "#pricing";
    public string SecondaryCtaLinkType { get; set; } = "section";
    public string BackgroundImageUrl { get; set; } = "/images/poolhub/hero.png";
    public string? BackgroundVideoUrl { get; set; }
    public bool UseVideo { get; set; }
    public string FallbackImageUrl { get; set; } = "/images/poolhub/hero.png";
    public List<string> Badges { get; set; } = [];
}

public class OrderedLandingItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UspItemDto : OrderedLandingItemDto
{
    public string? Icon { get; set; }
}

public class ServiceHighlightDto : OrderedLandingItemDto
{
    public string? PriceText { get; set; }
    public string? CtaText { get; set; }
    public string? CtaLink { get; set; }
    public string CtaLinkType { get; set; } = "section";
}

public class PricingHighlightDto : OrderedLandingItemDto
{
    public string PriceText { get; set; } = string.Empty;
    public string? TableType { get; set; }
    public string? TimeRange { get; set; }
    public string? Badge { get; set; }
}

public class GalleryItemDto : OrderedLandingItemDto
{
    public new string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string ImageType { get; set; } = "gallery";
}

public class ReviewDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    [Range(1, 5)] public int Rating { get; set; } = 5;
    public string Content { get; set; } = string.Empty;
    public string? CheckInImageUrl { get; set; }
    public bool IsFeatured { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class SocialLinkDto
{
    public string Platform { get; set; } = "Facebook";
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class BookingPolicyDto
{
    public bool AllowOnlineBooking { get; set; } = true;
    public int HoldMinutes { get; set; } = 15;
    public int DefaultDurationMinutes { get; set; } = 120;
    public int MinDurationMinutes { get; set; } = 60;
    public int MaxDurationMinutes { get; set; } = 240;
    public int AdvanceBookingDays { get; set; } = 14;
    public string SuccessMessage { get; set; } = "Yêu cầu đặt bàn đã được gửi. Nhân viên sẽ xác nhận trong thời gian sớm nhất.";
    public string PolicyNote { get; set; } = "Vui lòng đến trước giờ hẹn 10 phút.";
}

public class SeoSettingsDto
{
    public string MetaTitle { get; set; } = "PoolHub - Đặt bàn bi-a online nhanh chóng";
    public string MetaDescription { get; set; } = "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại, đồ uống đa dạng và đặt bàn online.";
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
}
