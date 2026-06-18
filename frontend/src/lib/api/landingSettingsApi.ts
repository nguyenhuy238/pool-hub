import { apiFetch } from "@/lib/api/client";

export type GeneralInfoSettings = {
  centerName: string;
  slogan: string;
  shortDescription: string;
  hotline: string;
  email: string;
  address: string;
  openingHours: string;
  facebookUrl?: string;
  tikTokUrl?: string;
  zaloUrl?: string;
  googleMapsUrl?: string;
  googleMapsEmbedUrl?: string;
  googleMapsShareUrl?: string;
  googleMapsDirectionUrl?: string;
  latitude?: number;
  longitude?: number;
  placeId?: string;
  mapDisplayMode: "embed" | "placeholder" | "external";
  logoUrl?: string;
  faviconUrl?: string;
};

export type PromotionBannerSettings = {
  isEnabled: boolean;
  content: string;
  ctaText: string;
  ctaLink: string;
  ctaLinkType: LinkType;
  startAtUtc?: string;
  endAtUtc?: string;
};

export type HeroSettings = {
  subtitle: string;
  title: string;
  description: string;
  primaryCtaText: string;
  primaryCtaLink: string;
  primaryCtaLinkType: LinkType;
  secondaryCtaText: string;
  secondaryCtaLink: string;
  secondaryCtaLinkType: LinkType;
  backgroundImageUrl: string;
  backgroundVideoUrl?: string;
  useVideo: boolean;
  fallbackImageUrl: string;
  badges: string[];
};

export type OrderedLandingItem = {
  title: string;
  description: string;
  imageUrl?: string;
  displayOrder: number;
  isActive: boolean;
};

export type UspSettings = OrderedLandingItem & { icon?: string };
export type ServiceSettings = OrderedLandingItem & { priceText?: string; ctaText?: string; ctaLink?: string; ctaLinkType: LinkType };
export type PricingHighlightSettings = OrderedLandingItem & { priceText: string; tableType?: string; timeRange?: string; badge?: string };
export type GallerySettings = OrderedLandingItem & { imageUrl: string; altText: string; caption?: string; imageType: string };
export type ReviewSettings = {
  customerName: string;
  avatarUrl?: string;
  rating: number;
  content: string;
  checkInImageUrl?: string;
  isFeatured: boolean;
  isActive: boolean;
  displayOrder: number;
};

export type LinkType = "section" | "internal" | "external" | "phone" | "map" | "email";

export type SocialLinkSettings = {
  platform: string;
  url: string;
  icon: string;
  isActive: boolean;
  displayOrder: number;
};

export type BookingPolicySettings = {
  allowOnlineBooking: boolean;
  holdMinutes: number;
  defaultDurationMinutes: number;
  minDurationMinutes: number;
  maxDurationMinutes: number;
  advanceBookingDays: number;
  successMessage: string;
  policyNote: string;
};

export type SeoSettings = {
  metaTitle: string;
  metaDescription: string;
  metaKeywords?: string;
  ogImageUrl?: string;
  canonicalUrl?: string;
};

export type LandingPageSettings = {
  generalInfo: GeneralInfoSettings;
  promotionBanner: PromotionBannerSettings;
  hero: HeroSettings;
  uspItems: UspSettings[];
  services: ServiceSettings[];
  pricingHighlights: PricingHighlightSettings[];
  gallery: GallerySettings[];
  reviews: ReviewSettings[];
  socialLinks: SocialLinkSettings[];
  bookingPolicy: BookingPolicySettings;
  seo: SeoSettings;
};

export const defaultLandingSettings: LandingPageSettings = {
  generalInfo: {
    centerName: "PoolHub Center",
    slogan: "Billiards & Entertainment",
    shortDescription: "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại và đặt bàn online.",
    hotline: "0901 234 567",
    email: "hello@poolhub.vn",
    address: "123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh",
    openingHours: "09:00 - 24:00 hằng ngày",
    googleMapsUrl: "https://www.google.com/maps",
    googleMapsShareUrl: "https://www.google.com/maps",
    googleMapsDirectionUrl: "https://www.google.com/maps",
    mapDisplayMode: "placeholder"
  },
  promotionBanner: {
    isEnabled: true,
    content: "Tặng 30 phút chơi khi đặt bàn qua Web | Giảm 10% cho nhóm từ 4 người",
    ctaText: "Đặt ngay",
    ctaLink: "#booking",
    ctaLinkType: "section"
  },
  hero: {
    subtitle: "PoolHub Billiards & Entertainment",
    title: "Đặt bàn bi-a nhanh chóng - Trải nghiệm giải trí đẳng cấp",
    description: "Không gian hiện đại, bàn chuẩn, đồ uống phục vụ tận bàn và đặt lịch online để nhóm bạn đến là có bàn chơi.",
    primaryCtaText: "Đặt bàn ngay",
    primaryCtaLink: "#booking",
    primaryCtaLinkType: "section",
    secondaryCtaText: "Xem bảng giá",
    secondaryCtaLink: "#pricing",
    secondaryCtaLinkType: "section",
    backgroundImageUrl: "/images/poolhub/hero.png",
    fallbackImageUrl: "/images/poolhub/hero.png",
    useVideo: false,
    badges: ["12+ bàn sẵn sàng", "09:00 mở cửa mỗi ngày", "4.8/5 đánh giá khách"]
  },
  uspItems: [
    { title: "Bàn chuẩn thi đấu", description: "Mặt bàn, bóng và cơ gậy được kiểm tra định kỳ.", displayOrder: 1, isActive: true },
    { title: "Không gian thoải mái", description: "Điều hòa, wifi mạnh, khu vực chờ riêng.", displayOrder: 2, isActive: true },
    { title: "Phục vụ tận bàn", description: "Đồ uống, đồ ăn nhẹ và combo nhóm được mang tới bàn nhanh.", displayOrder: 3, isActive: true },
    { title: "Đặt bàn online", description: "Chọn ngày giờ, loại bàn và gửi yêu cầu trước.", displayOrder: 4, isActive: true }
  ],
  services: [
    { title: "Billiard / Pool", description: "Bàn pool phổ thông và bàn VIP.", imageUrl: "/images/poolhub/hero.png", priceText: "Từ 80.000đ/giờ", ctaText: "Đặt bàn", ctaLink: "#booking", ctaLinkType: "section", displayOrder: 1, isActive: true },
    { title: "Carom & Snooker", description: "Khu vực yên tĩnh cho người chơi kỹ thuật.", imageUrl: "/images/poolhub/hero.png", priceText: "Từ 90.000đ/giờ", ctaText: "Đặt bàn", ctaLink: "#booking", ctaLinkType: "section", displayOrder: 2, isActive: true },
    { title: "Đồ uống & snack", description: "Cà phê, trà trái cây, nước ngọt và combo nhóm.", imageUrl: "/images/poolhub/hero.png", priceText: "Từ 25.000đ", ctaText: "Xem thêm", ctaLink: "#contact", ctaLinkType: "section", displayOrder: 3, isActive: true }
  ],
  pricingHighlights: [
    { title: "Giờ vàng", description: "Thứ 2 - Thứ 6 trước 17h", priceText: "Từ 70.000đ/giờ", timeRange: "09:00 - 17:00", badge: "Giờ vàng", displayOrder: 1, isActive: true },
    { title: "Pool VIP", description: "Không gian riêng cho nhóm", priceText: "Từ 120.000đ/giờ", tableType: "VIP", badge: "Best choice", displayOrder: 2, isActive: true }
  ],
  gallery: [
    { title: "Khu bàn pool", description: "", imageUrl: "/images/poolhub/hero.png", altText: "Khu vực bàn pool hiện đại", displayOrder: 1, imageType: "gallery", isActive: true },
    { title: "Quầy bar", description: "", imageUrl: "/images/poolhub/hero.png", altText: "Quầy bar PoolHub", displayOrder: 2, imageType: "gallery", isActive: true }
  ],
  reviews: [
    { customerName: "Minh Quân", rating: 5, content: "Đặt bàn trên web nhanh, tới nơi là có bàn sẵn.", isFeatured: true, isActive: true, displayOrder: 1 },
    { customerName: "Thảo Vy", rating: 5, content: "Đồ uống lên nhanh và nhân viên xác nhận lịch rất gọn.", isFeatured: true, isActive: true, displayOrder: 2 },
    { customerName: "Hoàng Nam", rating: 4, content: "Bàn VIP chơi ổn, cơ gậy mới.", isFeatured: true, isActive: true, displayOrder: 3 }
  ],
  socialLinks: [
    { platform: "Facebook", url: "https://www.facebook.com", icon: "f", isActive: false, displayOrder: 1 },
    { platform: "TikTok", url: "https://www.tiktok.com", icon: "t", isActive: false, displayOrder: 2 }
  ],
  bookingPolicy: {
    allowOnlineBooking: true,
    holdMinutes: 15,
    defaultDurationMinutes: 120,
    minDurationMinutes: 60,
    maxDurationMinutes: 240,
    advanceBookingDays: 14,
    successMessage: "Yêu cầu đặt bàn đã được gửi. Nhân viên sẽ xác nhận trong thời gian sớm nhất.",
    policyNote: "Vui lòng đến trước giờ hẹn 10 phút."
  },
  seo: {
    metaTitle: "PoolHub - Đặt bàn bi-a online nhanh chóng",
    metaDescription: "Trung tâm bi-a và giải trí với bàn chuẩn, không gian hiện đại, đồ uống đa dạng và đặt bàn online."
  }
};

export function activeSorted<T extends { displayOrder: number; isActive?: boolean }>(items: T[]) {
  return [...items].filter((item) => item.isActive !== false).sort((a, b) => a.displayOrder - b.displayOrder);
}

export const landingSettingsApi = {
  public: async () => apiFetch<LandingPageSettings>("/api/public/landing-page", { skipAuth: true }),
  admin: async () => apiFetch<LandingPageSettings>("/api/admin/landing-page-settings"),
  update: async (body: LandingPageSettings) => apiFetch<LandingPageSettings>("/api/admin/landing-page-settings", { method: "PUT", body: JSON.stringify(body) }),
  resetDefault: async () => apiFetch<LandingPageSettings>("/api/admin/landing-page-settings/reset-default", { method: "POST" })
};
