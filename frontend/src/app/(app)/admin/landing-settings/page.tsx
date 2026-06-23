"use client";

import { useEffect, useState } from "react";
import { PageHeader, StateBlock } from "@/components/ui";
import { useToast } from "@/components/toast";
import { defaultLandingSettings, landingSettingsApi, type LandingPageSettings } from "@/lib/api/landingSettingsApi";
import { GoogleMapPicker, validateMap } from "@/components/admin/settings/GoogleMapPicker";
import { LinkPicker, validateLink } from "@/components/admin/settings/LinkPicker";
import { MediaPicker, validateMediaValue } from "@/components/admin/settings/MediaPicker";
import { SocialLinksEditor } from "@/components/admin/settings/SocialLinksEditor";

const tabs = [
  "Thông tin chung",
  "Banner khuyến mãi",
  "Hero Section",
  "About & Footer",
  "Lợi ích nổi bật",
  "Dịch vụ & Menu",
  "Bảng giá nổi bật",
  "Gallery",
  "Review khách hàng",
  "Chính sách đặt bàn",
  "SEO",
  "Legal, QR & Theme"
];

type ListName = "uspItems" | "services" | "pricingHighlights" | "gallery" | "reviews";

export default function LandingSettingsPage() {
  const toast = useToast();
  const [settings, setSettings] = useState<LandingPageSettings>(defaultLandingSettings);
  const [activeTab, setActiveTab] = useState(tabs[0]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    landingSettingsApi.admin()
      .then(setSettings)
      .catch((err) => setError(err instanceof Error ? err.message : "Không tải được cấu hình."))
      .finally(() => setLoading(false));
  }, []);

  function patch<T extends keyof LandingPageSettings>(key: T, value: LandingPageSettings[T]) {
    setSettings((current) => ({ ...current, [key]: value }));
  }

  function updateList<T extends ListName>(name: T, index: number, value: LandingPageSettings[T][number]) {
    setSettings((current) => {
      const next = [...current[name]] as LandingPageSettings[T];
      next[index] = value as never;
      return { ...current, [name]: next };
    });
  }

  function addItem(name: ListName) {
    const order = settings[name].length + 1;
    const defaults = {
      uspItems: { title: "Lợi ích mới", description: "", displayOrder: order, isActive: true, icon: "+" },
      services: { title: "Dịch vụ mới", description: "", imageUrl: "/images/poolhub/hero.png", priceText: "", ctaText: "Đặt bàn", ctaLink: "#booking", ctaLinkType: "section", displayOrder: order, isActive: true },
      pricingHighlights: { title: "Gói giá mới", description: "", priceText: "", tableType: "", timeRange: "", badge: "", displayOrder: order, isActive: true },
      gallery: { title: "Ảnh mới", description: "", imageUrl: "/images/poolhub/hero.png", altText: "Ảnh PoolHub", caption: "", imageType: "gallery", displayOrder: order, isActive: true },
      reviews: { customerName: "Khách hàng", rating: 5, content: "", isFeatured: true, isActive: true, displayOrder: order }
    };
    setSettings((current) => ({ ...current, [name]: [...current[name], defaults[name] as never] }));
  }

  function removeItem(name: ListName, index: number) {
    setSettings((current) => ({ ...current, [name]: current[name].filter((_, itemIndex) => itemIndex !== index) as never }));
  }

  async function save() {
    if (!settings.generalInfo.centerName.trim() || !settings.hero.title.trim()) {
      toast("Tên trung tâm và Hero title là bắt buộc.", "error");
      return;
    }
    const validationErrors = [
      validateMediaValue(settings.hero.backgroundImageUrl, "image", true),
      validateMediaValue(settings.hero.fallbackImageUrl, "image", settings.hero.useVideo),
      validateMediaValue(settings.hero.backgroundVideoUrl || "", "video", settings.hero.useVideo),
      validateMediaValue(settings.generalInfo.logoUrl || "", "image"),
      validateMediaValue(settings.generalInfo.faviconUrl || "", "image"),
      validateMediaValue(settings.seo.ogImageUrl || "", "image"),
      validateLink(settings.promotionBanner.ctaLinkType, settings.promotionBanner.ctaLink),
      validateLink(settings.hero.primaryCtaLinkType, settings.hero.primaryCtaLink),
      validateLink(settings.hero.secondaryCtaLinkType, settings.hero.secondaryCtaLink),
      ...validateMap(settings.generalInfo),
      ...settings.services.map((item) => validateMediaValue(item.imageUrl || "", "image", item.isActive)),
      ...settings.gallery.map((item) => validateMediaValue(item.imageUrl || "", "image", item.isActive)),
      ...settings.reviews.flatMap((item) => [
        validateMediaValue(item.avatarUrl || "", "image"),
        validateMediaValue(item.checkInImageUrl || "", "image")
      ])
    ].filter(Boolean);
    if (validationErrors.length) {
      toast(validationErrors[0], "error");
      return;
    }

    setSaving(true);
    try {
      setSettings(await landingSettingsApi.update(settings));
      toast("Đã lưu cấu hình Landing Page.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Lưu cấu hình thất bại.", "error");
    } finally {
      setSaving(false);
    }
  }

  async function resetDefault() {
    if (!window.confirm("Reset Landing Page về dữ liệu mặc định?")) return;
    setSaving(true);
    try {
      setSettings(await landingSettingsApi.resetDefault());
      toast("Đã reset cấu hình mặc định.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Reset thất bại.", "error");
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Landing Page Settings"
        description="Cấu hình nội dung hiển thị ngoài trang chủ public."
        action={<div className="actions"><a className="ghost-btn" href="/" target="_blank">Preview</a><button className="secondary-btn" onClick={resetDefault} disabled={saving}>Reset default</button><button className="primary-btn" onClick={save} disabled={saving}>{saving ? "Đang lưu..." : "Save"}</button></div>}
      />
      <StateBlock loading={loading} error={error} />
      {!loading ? (
        <div className="settings-layout">
          <div className="settings-tabs">
            {tabs.map((tab) => <button key={tab} className={activeTab === tab ? "active" : ""} onClick={() => setActiveTab(tab)}>{tab}</button>)}
          </div>
          <div className="card settings-panel">
            {activeTab === "Thông tin chung" ? (
              <FormGrid>
                <Text label="Tên trung tâm" value={settings.generalInfo.centerName} onChange={(centerName) => patch("generalInfo", { ...settings.generalInfo, centerName })} />
                <Text label="Slogan" value={settings.generalInfo.slogan} onChange={(slogan) => patch("generalInfo", { ...settings.generalInfo, slogan })} />
                <Area label="Mô tả ngắn" value={settings.generalInfo.shortDescription} onChange={(shortDescription) => patch("generalInfo", { ...settings.generalInfo, shortDescription })} />
                <Text label="Hotline" value={settings.generalInfo.hotline} onChange={(hotline) => patch("generalInfo", { ...settings.generalInfo, hotline })} />
                <Text label="Email" value={settings.generalInfo.email} onChange={(email) => patch("generalInfo", { ...settings.generalInfo, email })} />
                <Text label="Địa chỉ" value={settings.generalInfo.address} onChange={(address) => patch("generalInfo", { ...settings.generalInfo, address })} />
                <Text label="Giờ mở cửa" value={settings.generalInfo.openingHours} onChange={(openingHours) => patch("generalInfo", { ...settings.generalInfo, openingHours })} />
                <MediaPicker label="Logo" folder="logo" value={settings.generalInfo.logoUrl || ""} onChange={(logoUrl) => patch("generalInfo", { ...settings.generalInfo, logoUrl })} />
                <MediaPicker label="Favicon" folder="logo" value={settings.generalInfo.faviconUrl || ""} onChange={(faviconUrl) => patch("generalInfo", { ...settings.generalInfo, faviconUrl })} helperText="Hỗ trợ PNG hoặc ICO. SVG không được chấp nhận nếu chưa sanitize." />
                <GoogleMapPicker value={settings.generalInfo} onChange={(generalInfo) => patch("generalInfo", generalInfo)} />
                <SocialLinksEditor items={settings.socialLinks || []} onChange={(socialLinks) => patch("socialLinks", socialLinks)} />
              </FormGrid>
            ) : null}

            {activeTab === "Banner khuyến mãi" ? (
              <FormGrid>
                <Check label="Bật banner" checked={settings.promotionBanner.isEnabled} onChange={(isEnabled) => patch("promotionBanner", { ...settings.promotionBanner, isEnabled })} />
                <Text label="Nội dung" value={settings.promotionBanner.content} onChange={(content) => patch("promotionBanner", { ...settings.promotionBanner, content })} />
                <Text label="CTA text" value={settings.promotionBanner.ctaText} onChange={(ctaText) => patch("promotionBanner", { ...settings.promotionBanner, ctaText })} />
                <LinkPicker label="CTA link" type={settings.promotionBanner.ctaLinkType} value={settings.promotionBanner.ctaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(ctaLinkType, ctaLink) => patch("promotionBanner", { ...settings.promotionBanner, ctaLinkType, ctaLink })} />
              </FormGrid>
            ) : null}

            {activeTab === "Hero Section" ? (
              <FormGrid>
                <Text label="Subtitle" value={settings.hero.subtitle} onChange={(subtitle) => patch("hero", { ...settings.hero, subtitle })} />
                <Text label="Title" value={settings.hero.title} onChange={(title) => patch("hero", { ...settings.hero, title })} />
                <Area label="Description" value={settings.hero.description} onChange={(description) => patch("hero", { ...settings.hero, description })} />
                <Text label="Primary CTA text" value={settings.hero.primaryCtaText} onChange={(primaryCtaText) => patch("hero", { ...settings.hero, primaryCtaText })} />
                <LinkPicker label="Primary CTA link" type={settings.hero.primaryCtaLinkType} value={settings.hero.primaryCtaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(primaryCtaLinkType, primaryCtaLink) => patch("hero", { ...settings.hero, primaryCtaLinkType, primaryCtaLink })} />
                <Text label="Secondary CTA text" value={settings.hero.secondaryCtaText} onChange={(secondaryCtaText) => patch("hero", { ...settings.hero, secondaryCtaText })} />
                <LinkPicker label="Secondary CTA link" type={settings.hero.secondaryCtaLinkType} value={settings.hero.secondaryCtaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(secondaryCtaLinkType, secondaryCtaLink) => patch("hero", { ...settings.hero, secondaryCtaLinkType, secondaryCtaLink })} />
                <MediaPicker label="Background image" folder="hero" value={settings.hero.backgroundImageUrl} required onChange={(backgroundImageUrl) => patch("hero", { ...settings.hero, backgroundImageUrl })} />
                <MediaPicker label="Background video" folder="hero" mediaType="video" value={settings.hero.backgroundVideoUrl || ""} required={settings.hero.useVideo} onChange={(backgroundVideoUrl) => patch("hero", { ...settings.hero, backgroundVideoUrl })} />
                <MediaPicker label="Fallback image" folder="hero" value={settings.hero.fallbackImageUrl} required={settings.hero.useVideo} onChange={(fallbackImageUrl) => patch("hero", { ...settings.hero, fallbackImageUrl })} />
                <Check label="Dùng video hero" checked={settings.hero.useVideo} onChange={(useVideo) => patch("hero", { ...settings.hero, useVideo })} />
                <Text label="Hero badges, cách nhau bằng dấu |" value={settings.hero.badges.join(" | ")} onChange={(value) => patch("hero", { ...settings.hero, badges: value.split("|").map((item) => item.trim()).filter(Boolean) })} />
              </FormGrid>
            ) : null}

            {activeTab === "About & Footer" ? <FormGrid>
              <Check label="Hiển thị About" checked={settings.about.isEnabled} onChange={(isEnabled) => patch("about", { ...settings.about, isEnabled })} />
              <Text label="About eyebrow" value={settings.about.eyebrow} onChange={(eyebrow) => patch("about", { ...settings.about, eyebrow })} />
              <Text label="About title" value={settings.about.title} onChange={(title) => patch("about", { ...settings.about, title })} />
              <Area label="About description" value={settings.about.description} onChange={(description) => patch("about", { ...settings.about, description })} />
              <MediaPicker label="About image" folder="about" value={settings.about.imageUrl || ""} onChange={(imageUrl) => patch("about", { ...settings.about, imageUrl })} />
              <Text label="Footer menu title" value={settings.footer.menuTitle} onChange={(menuTitle) => patch("footer", { ...settings.footer, menuTitle })} />
              <Text label="Footer policy title" value={settings.footer.policyTitle} onChange={(policyTitle) => patch("footer", { ...settings.footer, policyTitle })} />
              <Text label="Copyright" value={settings.footer.copyright} onChange={(copyright) => patch("footer", { ...settings.footer, copyright })} />
            </FormGrid> : null}

            {activeTab === "Lợi ích nổi bật" ? <Repeater title="Lợi ích" name="uspItems" items={settings.uspItems} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Icon" value={item.icon || ""} onChange={(icon) => updateList("uspItems", index, { ...item, icon })} /><Text label="Title" value={item.title} onChange={(title) => updateList("uspItems", index, { ...item, title })} /><Area label="Description" value={item.description} onChange={(description) => updateList("uspItems", index, { ...item, description })} /><NumberInput label="Display order" value={item.displayOrder} onChange={(displayOrder) => updateList("uspItems", index, { ...item, displayOrder })} /><Check label="Active" checked={item.isActive} onChange={(isActive) => updateList("uspItems", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}
            {activeTab === "Dịch vụ & Menu" ? (
              <Repeater title="Dịch vụ" name="services" items={settings.services} onAdd={addItem} onRemove={removeItem}>
                {(item, index) => (
                  <FormGrid>
                    <Text label="Title" value={item.title} onChange={(title) => updateList("services", index, { ...item, title })} />
                    <Area label="Description" value={item.description} onChange={(description) => updateList("services", index, { ...item, description })} />
                    <MediaPicker label="Service image" folder="services" value={item.imageUrl || ""} required={item.isActive} altText={item.title} onChange={(imageUrl) => updateList("services", index, { ...item, imageUrl })} />
                    <Text label="Giá tham khảo" value={item.priceText || ""} onChange={(priceText) => updateList("services", index, { ...item, priceText })} />
                    <Text label="CTA text" value={item.ctaText || ""} onChange={(ctaText) => updateList("services", index, { ...item, ctaText })} />
                    <LinkPicker label="CTA link" type={item.ctaLinkType || "section"} value={item.ctaLink || "#booking"} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(ctaLinkType, ctaLink) => updateList("services", index, { ...item, ctaLinkType, ctaLink })} />
                    <NumberInput label="Display order" value={item.displayOrder} onChange={(displayOrder) => updateList("services", index, { ...item, displayOrder })} />
                    <Check label="Active" checked={item.isActive} onChange={(isActive) => updateList("services", index, { ...item, isActive })} />
                  </FormGrid>
                )}
              </Repeater>
            ) : null}
            {activeTab === "Bảng giá nổi bật" ? <Repeater title="Bảng giá" name="pricingHighlights" items={settings.pricingHighlights} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Title" value={item.title} onChange={(title) => updateList("pricingHighlights", index, { ...item, title })} /><Area label="Description" value={item.description} onChange={(description) => updateList("pricingHighlights", index, { ...item, description })} /><Text label="Giá hiển thị" value={item.priceText} onChange={(priceText) => updateList("pricingHighlights", index, { ...item, priceText })} /><Text label="Loại bàn" value={item.tableType || ""} onChange={(tableType) => updateList("pricingHighlights", index, { ...item, tableType })} /><Text label="Khung giờ" value={item.timeRange || ""} onChange={(timeRange) => updateList("pricingHighlights", index, { ...item, timeRange })} /><Text label="Nhãn nổi bật" value={item.badge || ""} onChange={(badge) => updateList("pricingHighlights", index, { ...item, badge })} /><NumberInput label="Display order" value={item.displayOrder} onChange={(displayOrder) => updateList("pricingHighlights", index, { ...item, displayOrder })} /><Check label="Active" checked={item.isActive} onChange={(isActive) => updateList("pricingHighlights", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}
            {activeTab === "Gallery" ? <Repeater title="Gallery" name="gallery" items={settings.gallery} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Title" value={item.title} onChange={(title) => updateList("gallery", index, { ...item, title })} /><MediaPicker label="Gallery image" folder="gallery" value={item.imageUrl} required={item.isActive} altText={item.altText} onChange={(imageUrl) => updateList("gallery", index, { ...item, imageUrl })} /><Text label="Alt text" value={item.altText} onChange={(altText) => updateList("gallery", index, { ...item, altText })} /><Text label="Caption" value={item.caption || ""} onChange={(caption) => updateList("gallery", index, { ...item, caption })} /><Text label="Loại ảnh" value={item.imageType} onChange={(imageType) => updateList("gallery", index, { ...item, imageType })} /><NumberInput label="Display order" value={item.displayOrder} onChange={(displayOrder) => updateList("gallery", index, { ...item, displayOrder })} /><Check label="Active" checked={item.isActive} onChange={(isActive) => updateList("gallery", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}
            {activeTab === "Review khách hàng" ? <Repeater title="Review" name="reviews" items={settings.reviews} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Tên khách" value={item.customerName} onChange={(customerName) => updateList("reviews", index, { ...item, customerName })} /><MediaPicker label="Avatar" folder="reviews" value={item.avatarUrl || ""} altText={item.customerName} onChange={(avatarUrl) => updateList("reviews", index, { ...item, avatarUrl })} /><MediaPicker label="Ảnh check-in" folder="reviews" value={item.checkInImageUrl || ""} altText={item.customerName} onChange={(checkInImageUrl) => updateList("reviews", index, { ...item, checkInImageUrl })} /><NumberInput label="Rating" value={item.rating} onChange={(rating) => updateList("reviews", index, { ...item, rating })} /><Area label="Nội dung" value={item.content} onChange={(content) => updateList("reviews", index, { ...item, content })} /><NumberInput label="Display order" value={item.displayOrder} onChange={(displayOrder) => updateList("reviews", index, { ...item, displayOrder })} /><Check label="Featured" checked={item.isFeatured} onChange={(isFeatured) => updateList("reviews", index, { ...item, isFeatured })} /><Check label="Active" checked={item.isActive} onChange={(isActive) => updateList("reviews", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}
            {activeTab === "Chính sách đặt bàn" ? <FormGrid><Check label="Cho phép đặt online" checked={settings.bookingPolicy.allowOnlineBooking} onChange={(allowOnlineBooking) => patch("bookingPolicy", { ...settings.bookingPolicy, allowOnlineBooking })} /><NumberInput label="Giữ bàn sau giờ hẹn (phút)" value={settings.bookingPolicy.holdMinutes} onChange={(holdMinutes) => patch("bookingPolicy", { ...settings.bookingPolicy, holdMinutes })} /><NumberInput label="Thời lượng mặc định (phút)" value={settings.bookingPolicy.defaultDurationMinutes} onChange={(defaultDurationMinutes) => patch("bookingPolicy", { ...settings.bookingPolicy, defaultDurationMinutes })} /><NumberInput label="Thời lượng tối thiểu (phút)" value={settings.bookingPolicy.minDurationMinutes} onChange={(minDurationMinutes) => patch("bookingPolicy", { ...settings.bookingPolicy, minDurationMinutes })} /><NumberInput label="Thời lượng tối đa (phút)" value={settings.bookingPolicy.maxDurationMinutes} onChange={(maxDurationMinutes) => patch("bookingPolicy", { ...settings.bookingPolicy, maxDurationMinutes })} /><NumberInput label="Số ngày đặt trước" value={settings.bookingPolicy.advanceBookingDays} onChange={(advanceBookingDays) => patch("bookingPolicy", { ...settings.bookingPolicy, advanceBookingDays })} /><Area label="Thông báo sau khi đặt thành công" value={settings.bookingPolicy.successMessage} onChange={(successMessage) => patch("bookingPolicy", { ...settings.bookingPolicy, successMessage })} /><Area label="Ghi chú chính sách" value={settings.bookingPolicy.policyNote} onChange={(policyNote) => patch("bookingPolicy", { ...settings.bookingPolicy, policyNote })} /></FormGrid> : null}
            {activeTab === "SEO" ? <FormGrid><Text label="Meta title" value={settings.seo.metaTitle} onChange={(metaTitle) => patch("seo", { ...settings.seo, metaTitle })} /><Area label="Meta description" value={settings.seo.metaDescription} onChange={(metaDescription) => patch("seo", { ...settings.seo, metaDescription })} /><Text label="Meta keywords" value={settings.seo.metaKeywords || ""} onChange={(metaKeywords) => patch("seo", { ...settings.seo, metaKeywords })} /><MediaPicker label="OG image" folder="seo" value={settings.seo.ogImageUrl || ""} onChange={(ogImageUrl) => patch("seo", { ...settings.seo, ogImageUrl })} /><Text label="Canonical URL" value={settings.seo.canonicalUrl || ""} onChange={(canonicalUrl) => patch("seo", { ...settings.seo, canonicalUrl })} /></FormGrid> : null}
            {activeTab === "Legal, QR & Theme" ? <FormGrid>
              <Area label="Privacy policy" value={settings.legal.privacyPolicy} onChange={(privacyPolicy) => patch("legal", { ...settings.legal, privacyPolicy })} />
              <Area label="Terms of service" value={settings.legal.termsOfService} onChange={(termsOfService) => patch("legal", { ...settings.legal, termsOfService })} />
              <Text label="Primary color" value={settings.theme.primaryColor} onChange={(primaryColor) => patch("theme", { ...settings.theme, primaryColor })} />
              <Text label="Accent color" value={settings.theme.accentColor} onChange={(accentColor) => patch("theme", { ...settings.theme, accentColor })} />
              <Check label="Hiển thị QR code" checked={settings.qrCode.isEnabled} onChange={(isEnabled) => patch("qrCode", { ...settings.qrCode, isEnabled })} />
              <MediaPicker label="QR image" folder="qr" value={settings.qrCode.imageUrl || ""} onChange={(imageUrl) => patch("qrCode", { ...settings.qrCode, imageUrl })} />
              <Text label="QR caption" value={settings.qrCode.caption || ""} onChange={(caption) => patch("qrCode", { ...settings.qrCode, caption })} />
            </FormGrid> : null}
          </div>
        </div>
      ) : null}
    </>
  );
}

function FormGrid({ children }: { children: React.ReactNode }) {
  return <div className="settings-form-grid">{children}</div>;
}

function Text({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return <label><span>{label}</span><input value={value} onChange={(event) => onChange(event.target.value)} /></label>;
}

function Area({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  return <label className="full-field"><span>{label}</span><textarea rows={3} value={value} onChange={(event) => onChange(event.target.value)} /></label>;
}

function NumberInput({ label, value, onChange }: { label: string; value: number; onChange: (value: number) => void }) {
  return <label><span>{label}</span><input type="number" value={value} onChange={(event) => onChange(Number(event.target.value))} /></label>;
}

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="check-row"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /><span>{label}</span></label>;
}

function Repeater<T>({ title, name, items, children, onAdd, onRemove }: {
  title: string;
  name: ListName;
  items: T[];
  children: (item: T, index: number) => React.ReactNode;
  onAdd: (name: ListName) => void;
  onRemove: (name: ListName, index: number) => void;
}) {
  return (
    <div className="settings-repeater">
      <div className="repeater-head"><h2>{title}</h2><button className="primary-btn" onClick={() => onAdd(name)}>Thêm item</button></div>
      {items.map((item, index) => (
        <div className="repeater-item" key={index}>
          <div className="repeater-item-head"><strong>Item {index + 1}</strong><button className="danger-btn" onClick={() => onRemove(name, index)}>Xóa</button></div>
          {children(item, index)}
        </div>
      ))}
    </div>
  );
}
