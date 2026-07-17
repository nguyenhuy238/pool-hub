"use client";

import { useEffect, useMemo, useState } from "react";
import { Modal, PageHeader } from "@/components/ui";
import { useToast } from "@/components/toast";
import {
  defaultLandingSettings,
  landingSettingsApi,
  LANDING_PREVIEW_STORAGE_KEY,
  type LandingPageSettings,
  type LinkType
} from "@/lib/api/landingSettingsApi";
import { GoogleMapPicker, validateMap } from "@/components/admin/settings/GoogleMapPicker";
import { LinkPicker, validateLink } from "@/components/admin/settings/LinkPicker";
import { MediaPicker, validateMediaValue } from "@/components/admin/settings/MediaPicker";
import { SocialLinksEditor } from "@/components/admin/settings/SocialLinksEditor";

type SectionId =
  | "general"
  | "promotion"
  | "hero"
  | "about"
  | "usp"
  | "services"
  | "pricing"
  | "gallery"
  | "reviews"
  | "booking"
  | "seo"
  | "legal-theme";

type SectionConfig = {
  id: SectionId;
  label: string;
  disabled?: (settings: LandingPageSettings) => boolean;
};

const sections: SectionConfig[] = [
  { id: "general", label: "Thông tin chung" },
  { id: "promotion", label: "Banner khuyến mãi", disabled: (s: LandingPageSettings) => !s.promotionBanner.isEnabled },
  { id: "hero", label: "Hero" },
  { id: "about", label: "Giới thiệu" },
  { id: "usp", label: "Lợi ích nổi bật" },
  { id: "services", label: "Dịch vụ và menu" },
  { id: "pricing", label: "Bảng giá nổi bật" },
  { id: "gallery", label: "Thư viện ảnh" },
  { id: "reviews", label: "Đánh giá khách hàng" },
  { id: "booking", label: "Chính sách đặt bàn", disabled: (s: LandingPageSettings) => !s.bookingPolicy.allowOnlineBooking },
  { id: "seo", label: "SEO" },
  { id: "legal-theme", label: "Pháp lý, QR và giao diện" }
] ;
type ListName = "uspItems" | "services" | "pricingHighlights" | "gallery" | "reviews";
type ValidationIssue = { section: SectionId; message: string };

const sectionLabels = Object.fromEntries(sections.map((section) => [section.id, section.label])) as Record<SectionId, string>;

export default function LandingSettingsPage() {
  const toast = useToast();
  const [settings, setSettings] = useState<LandingPageSettings>(defaultLandingSettings);
  const [activeSection, setActiveSection] = useState<SectionId>("general");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [resetOpen, setResetOpen] = useState(false);
  const [dirtySections, setDirtySections] = useState<Set<SectionId>>(new Set());
  const [savedSections, setSavedSections] = useState<Set<SectionId>>(new Set());

  const dirty = dirtySections.size > 0;
  const issues = useMemo(() => validateLandingSettings(settings), [settings]);
  const issuesBySection = useMemo(() => groupIssues(issues), [issues]);

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!dirty) return;
    const warnBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", warnBeforeUnload);
    return () => window.removeEventListener("beforeunload", warnBeforeUnload);
  }, [dirty]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setSettings(await landingSettingsApi.admin());
      setDirtySections(new Set());
      setSavedSections(new Set());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không tải được cấu hình trang chủ.");
    } finally {
      setLoading(false);
    }
  }

  function markDirty(section: SectionId) {
    setDirtySections((current) => new Set(current).add(section));
    setSavedSections((current) => {
      const next = new Set(current);
      next.delete(section);
      return next;
    });
  }

  function patch<T extends keyof LandingPageSettings>(section: SectionId, key: T, value: LandingPageSettings[T]) {
    markDirty(section);
    setSettings((current) => ({ ...current, [key]: value }));
  }

  function updateList<T extends ListName>(section: SectionId, name: T, index: number, value: LandingPageSettings[T][number]) {
    markDirty(section);
    setSettings((current) => {
      const next = [...current[name]] as LandingPageSettings[T];
      next[index] = value as never;
      return { ...current, [name]: next };
    });
  }

  function addItem(section: SectionId, name: ListName) {
    markDirty(section);
    const order = settings[name].length + 1;
    const defaults = {
      uspItems: { title: "Lợi ích mới", description: "", displayOrder: order, isActive: true, icon: "star" },
      services: { title: "Dịch vụ mới", description: "", imageUrl: "/images/poolhub/hero.png", priceText: "", ctaText: "Đặt bàn", ctaLink: "#booking", ctaLinkType: "section" as LinkType, displayOrder: order, isActive: true },
      pricingHighlights: { title: "Gói giá mới", description: "", priceText: "", tableType: "", timeRange: "", badge: "", displayOrder: order, isActive: true },
      gallery: { title: "Ảnh mới", description: "", imageUrl: "/images/poolhub/hero.png", altText: "Ảnh PoolHub", caption: "", imageType: "gallery", displayOrder: order, isActive: true },
      reviews: { customerName: "Khách hàng", rating: 5, content: "", isFeatured: true, isActive: true, displayOrder: order }
    };
    setSettings((current) => ({ ...current, [name]: [...current[name], defaults[name] as never] }));
  }

  function removeItem(section: SectionId, name: ListName, index: number) {
    markDirty(section);
    setSettings((current) => ({ ...current, [name]: current[name].filter((_, itemIndex) => itemIndex !== index) as never }));
  }

  function preview() {
    const payload = JSON.stringify({ settings, expiresAt: Date.now() + 30 * 60 * 1000 });
    sessionStorage.setItem(LANDING_PREVIEW_STORAGE_KEY, payload);
    localStorage.setItem(LANDING_PREVIEW_STORAGE_KEY, payload);
    window.open("/?preview=landing-draft", "_blank", "noopener,noreferrer");
  }

  async function save() {
    if (saving) return;
    if (issues.length) {
      setActiveSection(issues[0].section);
      toast(`Vui lòng sửa: ${issues[0].message}`, "error");
      return;
    }

    setSaving(true);
    try {
      const saved = await landingSettingsApi.update(settings);
      setSettings(saved);
      setDirtySections(new Set());
      setSavedSections(new Set(sections.map((section) => section.id)));
      toast("Đã lưu thay đổi trang chủ.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Lưu cấu hình thất bại.", "error");
    } finally {
      setSaving(false);
    }
  }

  function restoreSection(section: SectionId) {
    const defaults = defaultLandingSettings;
    const next: LandingPageSettings = structuredClone(settings);
    if (section === "general") next.generalInfo = structuredClone(defaults.generalInfo);
    if (section === "promotion") next.promotionBanner = structuredClone(defaults.promotionBanner);
    if (section === "hero") next.hero = structuredClone(defaults.hero);
    if (section === "about") {
      next.about = structuredClone(defaults.about);
      next.footer = structuredClone(defaults.footer);
      next.socialLinks = structuredClone(defaults.socialLinks);
    }
    if (section === "usp") next.uspItems = structuredClone(defaults.uspItems);
    if (section === "services") next.services = structuredClone(defaults.services);
    if (section === "pricing") next.pricingHighlights = structuredClone(defaults.pricingHighlights);
    if (section === "gallery") next.gallery = structuredClone(defaults.gallery);
    if (section === "reviews") next.reviews = structuredClone(defaults.reviews);
    if (section === "booking") next.bookingPolicy = structuredClone(defaults.bookingPolicy);
    if (section === "seo") next.seo = structuredClone(defaults.seo);
    if (section === "legal-theme") {
      next.legal = structuredClone(defaults.legal);
      next.theme = structuredClone(defaults.theme);
      next.qrCode = structuredClone(defaults.qrCode);
      next.depositPayment = structuredClone(defaults.depositPayment);
    }
    setSettings(next);
    markDirty(section);
    setResetOpen(false);
    toast("Đã khôi phục bản nháp của section. Hãy xem trước rồi lưu để áp dụng.", "info");
  }

  function restoreAll() {
    setSettings(structuredClone(defaultLandingSettings));
    setDirtySections(new Set(sections.map((section) => section.id)));
    setSavedSections(new Set());
    setResetOpen(false);
    toast("Đã khôi phục bản nháp toàn bộ trang chủ. Hãy xem trước rồi lưu để áp dụng.", "info");
  }

  return (
    <>
      <PageHeader
        title="Cấu hình trang chủ"
        description={dirty ? `Có thay đổi chưa lưu ở ${dirtySections.size} section.` : "Quản lý nội dung trang chủ dành cho khách hàng."}
        action={<div className="actions"><button className="ghost-btn" type="button" onClick={preview}>Xem trước</button><button className="secondary-btn" type="button" onClick={() => setResetOpen(true)} disabled={saving}>Khôi phục mặc định</button><button className="primary-btn" type="button" onClick={save} disabled={saving || loading}>{saving ? "Đang lưu..." : "Lưu thay đổi"}</button></div>}
      />

      {loading ? <div className="state-card loading-state"><span className="spinner" />Đang tải cấu hình trang chủ...</div> : null}
      {error ? <div className="state-card error"><p>{error}</p><button type="button" className="ghost-btn" onClick={load}>Thử lại</button></div> : null}

      {!loading && !error ? (
        <div className="settings-layout">
          <nav className="settings-tabs" aria-label="Section cấu hình trang chủ">
            {sections.map((section) => {
              const hasErrors = Boolean(issuesBySection[section.id]?.length);
              const isDirty = dirtySections.has(section.id);
              const isSaved = savedSections.has(section.id);
              const isDisabled = section.disabled?.(settings) ?? false;
              return (
                <button key={section.id} type="button" className={activeSection === section.id ? "active" : ""} onClick={() => setActiveSection(section.id)} aria-current={activeSection === section.id ? "page" : undefined}>
                  <span>{section.label}</span>
                  <small className={`section-status ${hasErrors ? "error" : isDirty ? "dirty" : isSaved ? "saved" : isDisabled ? "disabled" : "clean"}`}>
                    {hasErrors ? "Có lỗi" : isDirty ? "Chưa lưu" : isSaved ? "Đã lưu" : isDisabled ? "Đang tắt" : "Chưa đổi"}
                  </small>
                </button>
              );
            })}
          </nav>

          <div className="card settings-panel">
            {issuesBySection[activeSection]?.length ? (
              <div className="inline-alert error" role="alert">
                {issuesBySection[activeSection].map((issue) => <p key={issue.message}>{issue.message}</p>)}
              </div>
            ) : null}

            {activeSection === "general" ? (
              <FormGrid>
                <Text label="Tên trung tâm" value={settings.generalInfo.centerName} onChange={(centerName) => patch("general", "generalInfo", { ...settings.generalInfo, centerName })} />
                <Text label="Khẩu hiệu" value={settings.generalInfo.slogan} onChange={(slogan) => patch("general", "generalInfo", { ...settings.generalInfo, slogan })} />
                <Area label="Mô tả ngắn" value={settings.generalInfo.shortDescription} onChange={(shortDescription) => patch("general", "generalInfo", { ...settings.generalInfo, shortDescription })} />
                <Text label="Hotline" value={settings.generalInfo.hotline} onChange={(hotline) => patch("general", "generalInfo", { ...settings.generalInfo, hotline })} />
                <Text label="Email" value={settings.generalInfo.email} onChange={(email) => patch("general", "generalInfo", { ...settings.generalInfo, email })} />
                <Text label="Địa chỉ" value={settings.generalInfo.address} onChange={(address) => patch("general", "generalInfo", { ...settings.generalInfo, address })} />
                <Text label="Giờ mở cửa" value={settings.generalInfo.openingHours} onChange={(openingHours) => patch("general", "generalInfo", { ...settings.generalInfo, openingHours })} />
                <MediaPicker label="Logo" folder="logo" value={settings.generalInfo.logoUrl || ""} onChange={(logoUrl) => patch("general", "generalInfo", { ...settings.generalInfo, logoUrl })} />
                <MediaPicker label="Favicon" folder="logo" value={settings.generalInfo.faviconUrl || ""} onChange={(faviconUrl) => patch("general", "generalInfo", { ...settings.generalInfo, faviconUrl })} helperText="Hỗ trợ PNG, ICO, JPG, WebP hoặc GIF. SVG chưa được chấp nhận." />
                <GoogleMapPicker value={settings.generalInfo} onChange={(generalInfo) => patch("general", "generalInfo", generalInfo)} />
              </FormGrid>
            ) : null}

            {activeSection === "promotion" ? (
              <FormGrid>
                <Check label="Hiển thị banner" checked={settings.promotionBanner.isEnabled} onChange={(isEnabled) => patch("promotion", "promotionBanner", { ...settings.promotionBanner, isEnabled })} />
                <Text label="Nội dung ngắn" value={settings.promotionBanner.content} onChange={(content) => patch("promotion", "promotionBanner", { ...settings.promotionBanner, content })} />
                <Text label="Nhãn nút" value={settings.promotionBanner.ctaText} onChange={(ctaText) => patch("promotion", "promotionBanner", { ...settings.promotionBanner, ctaText })} />
                <LinkPicker label="Liên kết của nút" type={settings.promotionBanner.ctaLinkType} value={settings.promotionBanner.ctaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(ctaLinkType, ctaLink) => patch("promotion", "promotionBanner", { ...settings.promotionBanner, ctaLinkType, ctaLink })} />
              </FormGrid>
            ) : null}

            {activeSection === "hero" ? (
              <FormGrid>
                <Text label="Nhãn nhỏ" value={settings.hero.subtitle} onChange={(subtitle) => patch("hero", "hero", { ...settings.hero, subtitle })} />
                <Text label="Tiêu đề" value={settings.hero.title} onChange={(title) => patch("hero", "hero", { ...settings.hero, title })} />
                <Area label="Mô tả" value={settings.hero.description} onChange={(description) => patch("hero", "hero", { ...settings.hero, description })} />
                <Text label="Nhãn nút chính" value={settings.hero.primaryCtaText} onChange={(primaryCtaText) => patch("hero", "hero", { ...settings.hero, primaryCtaText })} />
                <LinkPicker label="Liên kết nút chính" type={settings.hero.primaryCtaLinkType} value={settings.hero.primaryCtaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(primaryCtaLinkType, primaryCtaLink) => patch("hero", "hero", { ...settings.hero, primaryCtaLinkType, primaryCtaLink })} />
                <Text label="Nhãn nút phụ" value={settings.hero.secondaryCtaText} onChange={(secondaryCtaText) => patch("hero", "hero", { ...settings.hero, secondaryCtaText })} />
                <LinkPicker label="Liên kết nút phụ" type={settings.hero.secondaryCtaLinkType} value={settings.hero.secondaryCtaLink} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(secondaryCtaLinkType, secondaryCtaLink) => patch("hero", "hero", { ...settings.hero, secondaryCtaLinkType, secondaryCtaLink })} />
                <MediaPicker label="Ảnh nền hero" folder="hero" value={settings.hero.backgroundImageUrl} required onChange={(backgroundImageUrl) => patch("hero", "hero", { ...settings.hero, backgroundImageUrl })} />
                <Check label="Dùng video nền hero" checked={settings.hero.useVideo} onChange={(useVideo) => patch("hero", "hero", { ...settings.hero, useVideo })} />
                <MediaPicker label="Video nền hero" folder="hero" mediaType="video" value={settings.hero.backgroundVideoUrl || ""} required={settings.hero.useVideo} onChange={(backgroundVideoUrl) => patch("hero", "hero", { ...settings.hero, backgroundVideoUrl })} />
                <MediaPicker label="Ảnh dự phòng khi video lỗi" folder="hero" value={settings.hero.fallbackImageUrl} required={settings.hero.useVideo} onChange={(fallbackImageUrl) => patch("hero", "hero", { ...settings.hero, fallbackImageUrl })} />
                <BadgeEditor badges={settings.hero.badges} onChange={(badges) => patch("hero", "hero", { ...settings.hero, badges })} />
              </FormGrid>
            ) : null}

            {activeSection === "about" ? (
              <FormGrid>
                <Check label="Hiển thị section giới thiệu" checked={settings.about.isEnabled} onChange={(isEnabled) => patch("about", "about", { ...settings.about, isEnabled })} />
                <Text label="Nhãn section" value={settings.about.eyebrow} onChange={(eyebrow) => patch("about", "about", { ...settings.about, eyebrow })} />
                <Text label="Tiêu đề giới thiệu" value={settings.about.title} onChange={(title) => patch("about", "about", { ...settings.about, title })} />
                <Area label="Nội dung giới thiệu" value={settings.about.description} onChange={(description) => patch("about", "about", { ...settings.about, description })} />
                <MediaPicker label="Ảnh giới thiệu" folder="about" value={settings.about.imageUrl || ""} onChange={(imageUrl) => patch("about", "about", { ...settings.about, imageUrl })} />
                <Text label="Tiêu đề menu chân trang" value={settings.footer.menuTitle} onChange={(menuTitle) => patch("about", "footer", { ...settings.footer, menuTitle })} />
                <Text label="Tiêu đề chính sách chân trang" value={settings.footer.policyTitle} onChange={(policyTitle) => patch("about", "footer", { ...settings.footer, policyTitle })} />
                <Text label="Copyright" value={settings.footer.copyright} onChange={(copyright) => patch("about", "footer", { ...settings.footer, copyright })} />
                <SocialLinksEditor items={settings.socialLinks || []} onChange={(socialLinks) => patch("about", "socialLinks", socialLinks)} />
              </FormGrid>
            ) : null}

            {activeSection === "usp" ? <Repeater title="Lợi ích" section="usp" name="uspItems" items={settings.uspItems} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Icon được phép" value={item.icon || ""} onChange={(icon) => updateList("usp", "uspItems", index, { ...item, icon })} /><Text label="Tiêu đề" value={item.title} onChange={(title) => updateList("usp", "uspItems", index, { ...item, title })} /><Area label="Mô tả" value={item.description} onChange={(description) => updateList("usp", "uspItems", index, { ...item, description })} /><NumberInput label="Thứ tự hiển thị" value={item.displayOrder} onChange={(displayOrder) => updateList("usp", "uspItems", index, { ...item, displayOrder })} /><Check label="Đang hiển thị" checked={item.isActive} onChange={(isActive) => updateList("usp", "uspItems", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}

            {activeSection === "services" ? (
              <Repeater title="Dịch vụ" section="services" name="services" items={settings.services} onAdd={addItem} onRemove={removeItem}>
                {(item, index) => (
                  <FormGrid>
                    <Text label="Tiêu đề" value={item.title} onChange={(title) => updateList("services", "services", index, { ...item, title })} />
                    <Area label="Mô tả" value={item.description} onChange={(description) => updateList("services", "services", index, { ...item, description })} />
                    <MediaPicker label="Ảnh dịch vụ" folder="services" value={item.imageUrl || ""} required={item.isActive} altText={item.title} onChange={(imageUrl) => updateList("services", "services", index, { ...item, imageUrl })} />
                    <Text label="Giá tham khảo" value={item.priceText || ""} onChange={(priceText) => updateList("services", "services", index, { ...item, priceText })} />
                    <Text label="Nhãn nút" value={item.ctaText || ""} onChange={(ctaText) => updateList("services", "services", index, { ...item, ctaText })} />
                    <LinkPicker label="Liên kết nút" type={item.ctaLinkType || "section"} value={item.ctaLink || "#booking"} hotline={settings.generalInfo.hotline} email={settings.generalInfo.email} mapUrl={settings.generalInfo.googleMapsDirectionUrl} onChange={(ctaLinkType, ctaLink) => updateList("services", "services", index, { ...item, ctaLinkType, ctaLink })} />
                    <NumberInput label="Thứ tự hiển thị" value={item.displayOrder} onChange={(displayOrder) => updateList("services", "services", index, { ...item, displayOrder })} />
                    <Check label="Đang hiển thị" checked={item.isActive} onChange={(isActive) => updateList("services", "services", index, { ...item, isActive })} />
                  </FormGrid>
                )}
              </Repeater>
            ) : null}

            {activeSection === "pricing" ? <><div className="inline-alert warning">Trang chủ đang đọc giá thực tế từ module Bảng giá. Các mục bên dưới chỉ là dữ liệu marketing cũ và không được public section bảng giá sử dụng.</div><Repeater title="Bảng giá nổi bật cũ" section="pricing" name="pricingHighlights" items={settings.pricingHighlights} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Tiêu đề" value={item.title} onChange={(title) => updateList("pricing", "pricingHighlights", index, { ...item, title })} /><Area label="Mô tả" value={item.description} onChange={(description) => updateList("pricing", "pricingHighlights", index, { ...item, description })} /><Text label="Giá hiển thị" value={item.priceText} onChange={(priceText) => updateList("pricing", "pricingHighlights", index, { ...item, priceText })} /><Text label="Loại bàn" value={item.tableType || ""} onChange={(tableType) => updateList("pricing", "pricingHighlights", index, { ...item, tableType })} /><Text label="Khung giờ" value={item.timeRange || ""} onChange={(timeRange) => updateList("pricing", "pricingHighlights", index, { ...item, timeRange })} /><Text label="Nhãn nổi bật" value={item.badge || ""} onChange={(badge) => updateList("pricing", "pricingHighlights", index, { ...item, badge })} /><NumberInput label="Thứ tự hiển thị" value={item.displayOrder} onChange={(displayOrder) => updateList("pricing", "pricingHighlights", index, { ...item, displayOrder })} /><Check label="Đang hiển thị" checked={item.isActive} onChange={(isActive) => updateList("pricing", "pricingHighlights", index, { ...item, isActive })} /></FormGrid>}</Repeater></> : null}

            {activeSection === "gallery" ? <Repeater title="Thư viện ảnh" section="gallery" name="gallery" items={settings.gallery} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Tiêu đề" value={item.title} onChange={(title) => updateList("gallery", "gallery", index, { ...item, title })} /><MediaPicker label="Ảnh thư viện" folder="gallery" value={item.imageUrl} required={item.isActive} altText={item.altText} onChange={(imageUrl) => updateList("gallery", "gallery", index, { ...item, imageUrl })} /><Text label="Mô tả thay thế" value={item.altText} onChange={(altText) => updateList("gallery", "gallery", index, { ...item, altText })} /><Text label="Chú thích ảnh" value={item.caption || ""} onChange={(caption) => updateList("gallery", "gallery", index, { ...item, caption })} /><Text label="Loại ảnh" value={item.imageType} onChange={(imageType) => updateList("gallery", "gallery", index, { ...item, imageType })} /><NumberInput label="Thứ tự hiển thị" value={item.displayOrder} onChange={(displayOrder) => updateList("gallery", "gallery", index, { ...item, displayOrder })} /><Check label="Đang hiển thị" checked={item.isActive} onChange={(isActive) => updateList("gallery", "gallery", index, { ...item, isActive })} /></FormGrid>}</Repeater> : null}

            {activeSection === "reviews" ? <><div className="inline-alert warning">Trang chủ ưu tiên đánh giá thật đã được duyệt từ module Quản lý đánh giá. Danh sách dưới đây chỉ là fallback cũ khi chưa có đánh giá public.</div><Repeater title="Review fallback cũ" section="reviews" name="reviews" items={settings.reviews} onAdd={addItem} onRemove={removeItem}>{(item, index) => <FormGrid><Text label="Tên khách" value={item.customerName} onChange={(customerName) => updateList("reviews", "reviews", index, { ...item, customerName })} /><MediaPicker label="Ảnh đại diện" folder="reviews" value={item.avatarUrl || ""} altText={item.customerName} onChange={(avatarUrl) => updateList("reviews", "reviews", index, { ...item, avatarUrl })} /><MediaPicker label="Ảnh check-in" folder="reviews" value={item.checkInImageUrl || ""} altText={item.customerName} onChange={(checkInImageUrl) => updateList("reviews", "reviews", index, { ...item, checkInImageUrl })} /><NumberInput label="Số sao" value={item.rating} onChange={(rating) => updateList("reviews", "reviews", index, { ...item, rating })} /><Area label="Nội dung" value={item.content} onChange={(content) => updateList("reviews", "reviews", index, { ...item, content })} /><NumberInput label="Thứ tự hiển thị" value={item.displayOrder} onChange={(displayOrder) => updateList("reviews", "reviews", index, { ...item, displayOrder })} /><Check label="Đánh giá nổi bật" checked={item.isFeatured} onChange={(isFeatured) => updateList("reviews", "reviews", index, { ...item, isFeatured })} /><Check label="Đang hiển thị" checked={item.isActive} onChange={(isActive) => updateList("reviews", "reviews", index, { ...item, isActive })} /></FormGrid>}</Repeater></> : null}

            {activeSection === "booking" ? <FormGrid><div className="inline-alert warning full-field">Thay đổi tại đây chỉ là nội dung truyền thông trên trang chủ, không tự động thay đổi quy tắc xử lý booking trong backend.</div><Check label="Cho phép đặt online" checked={settings.bookingPolicy.allowOnlineBooking} onChange={(allowOnlineBooking) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, allowOnlineBooking })} /><NumberInput label="Giữ bàn sau giờ hẹn (phút)" value={settings.bookingPolicy.holdMinutes} onChange={(holdMinutes) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, holdMinutes })} /><NumberInput label="Thời lượng mặc định (phút)" value={settings.bookingPolicy.defaultDurationMinutes} onChange={(defaultDurationMinutes) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, defaultDurationMinutes })} /><NumberInput label="Thời lượng tối thiểu (phút)" value={settings.bookingPolicy.minDurationMinutes} onChange={(minDurationMinutes) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, minDurationMinutes })} /><NumberInput label="Thời lượng tối đa (phút)" value={settings.bookingPolicy.maxDurationMinutes} onChange={(maxDurationMinutes) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, maxDurationMinutes })} /><NumberInput label="Số ngày đặt trước" value={settings.bookingPolicy.advanceBookingDays} onChange={(advanceBookingDays) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, advanceBookingDays })} /><Area label="Thông báo sau khi đặt thành công" value={settings.bookingPolicy.successMessage} onChange={(successMessage) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, successMessage })} /><Area label="Ghi chú chính sách" value={settings.bookingPolicy.policyNote} onChange={(policyNote) => patch("booking", "bookingPolicy", { ...settings.bookingPolicy, policyNote })} /></FormGrid> : null}

            {activeSection === "seo" ? <FormGrid><Text label={`SEO title (${settings.seo.metaTitle.length}/60)`} value={settings.seo.metaTitle} onChange={(metaTitle) => patch("seo", "seo", { ...settings.seo, metaTitle })} /><Area label={`Meta description (${settings.seo.metaDescription.length}/160)`} value={settings.seo.metaDescription} onChange={(metaDescription) => patch("seo", "seo", { ...settings.seo, metaDescription })} /><Text label="Meta keywords" value={settings.seo.metaKeywords || ""} onChange={(metaKeywords) => patch("seo", "seo", { ...settings.seo, metaKeywords })} /><MediaPicker label="Open Graph image" folder="seo" value={settings.seo.ogImageUrl || ""} onChange={(ogImageUrl) => patch("seo", "seo", { ...settings.seo, ogImageUrl })} /><Text label="Canonical URL" value={settings.seo.canonicalUrl || ""} onChange={(canonicalUrl) => patch("seo", "seo", { ...settings.seo, canonicalUrl })} /></FormGrid> : null}

            {activeSection === "legal-theme" ? <FormGrid>
              <Area label="Chính sách riêng tư" value={settings.legal.privacyPolicy} onChange={(privacyPolicy) => patch("legal-theme", "legal", { ...settings.legal, privacyPolicy })} />
              <Area label="Điều khoản sử dụng" value={settings.legal.termsOfService} onChange={(termsOfService) => patch("legal-theme", "legal", { ...settings.legal, termsOfService })} />
              <Text label="Màu chính" value={settings.theme.primaryColor} onChange={(primaryColor) => patch("legal-theme", "theme", { ...settings.theme, primaryColor })} />
              <Text label="Màu nhấn" value={settings.theme.accentColor} onChange={(accentColor) => patch("legal-theme", "theme", { ...settings.theme, accentColor })} />
              <Check label="Hiển thị QR code" checked={settings.qrCode.isEnabled} onChange={(isEnabled) => patch("legal-theme", "qrCode", { ...settings.qrCode, isEnabled })} />
              <MediaPicker label="Ảnh QR" folder="qr" value={settings.qrCode.imageUrl || ""} onChange={(imageUrl) => patch("legal-theme", "qrCode", { ...settings.qrCode, imageUrl })} />
              <Text label="Chú thích QR" value={settings.qrCode.caption || ""} onChange={(caption) => patch("legal-theme", "qrCode", { ...settings.qrCode, caption })} />
            </FormGrid> : null}
          </div>
        </div>
      ) : null}

      {resetOpen ? (
        <Modal title="Khôi phục mặc định" onClose={() => setResetOpen(false)} size="small">
          <p className="modal-message">Chọn phạm vi khôi phục. Thao tác này chỉ đổi bản nháp trên màn hình, không xóa media vật lý và chưa áp dụng ra trang chủ cho tới khi bạn bấm Lưu thay đổi.</p>
          <div className="modal-actions">
            <button type="button" className="ghost-btn" onClick={() => setResetOpen(false)}>Hủy</button>
            <button type="button" className="secondary-btn" onClick={() => restoreSection(activeSection)}>Khôi phục section này</button>
            <button type="button" className="danger-btn" onClick={restoreAll}>Khôi phục toàn bộ trang chủ</button>
          </div>
        </Modal>
      ) : null}
    </>
  );
}

function validateLandingSettings(settings: LandingPageSettings): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const add = (section: SectionId, message: string) => issues.push({ section, message });
  if (!settings.generalInfo.centerName.trim()) add("general", "Tên trung tâm không được để trống.");
  if (settings.generalInfo.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(settings.generalInfo.email)) add("general", "Email không hợp lệ.");
  if (settings.generalInfo.hotline && !/^[0-9+()\s.-]{8,20}$/.test(settings.generalInfo.hotline)) add("general", "Hotline không hợp lệ.");
  if (settings.generalInfo.address && (settings.generalInfo.address.trim().length < 8 || !/[A-Za-zÀ-ỹ]/.test(settings.generalInfo.address))) add("general", "Địa chỉ cần có nội dung rõ ràng, không chỉ gồm số hoặc ký tự rác.");
  validateMap(settings.generalInfo).forEach((message) => add("general", message));
  addMediaIssue(issues, "general", settings.generalInfo.logoUrl || "", "image");
  addMediaIssue(issues, "general", settings.generalInfo.faviconUrl || "", "image");

  if (settings.promotionBanner.isEnabled) {
    if (!settings.promotionBanner.content.trim()) add("promotion", "Banner đang bật nên cần có nội dung.");
    addLinkIssue(issues, "promotion", settings.promotionBanner.ctaLinkType, settings.promotionBanner.ctaLink);
  }

  if (!settings.hero.title.trim()) add("hero", "Tiêu đề hero không được để trống.");
  addMediaIssue(issues, "hero", settings.hero.backgroundImageUrl, "image", true);
  addMediaIssue(issues, "hero", settings.hero.fallbackImageUrl, "image", settings.hero.useVideo);
  addMediaIssue(issues, "hero", settings.hero.backgroundVideoUrl || "", "video", settings.hero.useVideo);
  addLinkIssue(issues, "hero", settings.hero.primaryCtaLinkType, settings.hero.primaryCtaLink);
  addLinkIssue(issues, "hero", settings.hero.secondaryCtaLinkType, settings.hero.secondaryCtaLink);

  addMediaIssue(issues, "about", settings.about.imageUrl || "", "image");
  settings.services.forEach((item, index) => {
    if (item.isActive && !item.title.trim()) add("services", `Dịch vụ ${index + 1} thiếu tiêu đề.`);
    addMediaIssue(issues, "services", item.imageUrl || "", "image", item.isActive);
    if (item.ctaText?.trim()) addLinkIssue(issues, "services", item.ctaLinkType || "section", item.ctaLink || "");
  });
  settings.gallery.forEach((item, index) => {
    if (item.isActive && !item.altText.trim()) add("gallery", `Ảnh thư viện ${index + 1} thiếu mô tả thay thế.`);
    addMediaIssue(issues, "gallery", item.imageUrl, "image", item.isActive);
  });
  settings.reviews.forEach((item, index) => {
    if (item.rating < 1 || item.rating > 5) add("reviews", `Review ${index + 1} phải có số sao từ 1 đến 5.`);
    addMediaIssue(issues, "reviews", item.avatarUrl || "", "image");
    addMediaIssue(issues, "reviews", item.checkInImageUrl || "", "image");
  });
  if (settings.bookingPolicy.minDurationMinutes <= 0) add("booking", "Thời lượng tối thiểu phải lớn hơn 0.");
  if (settings.bookingPolicy.maxDurationMinutes < settings.bookingPolicy.minDurationMinutes) add("booking", "Thời lượng tối đa phải lớn hơn hoặc bằng thời lượng tối thiểu.");
  if (settings.bookingPolicy.defaultDurationMinutes < settings.bookingPolicy.minDurationMinutes || settings.bookingPolicy.defaultDurationMinutes > settings.bookingPolicy.maxDurationMinutes) add("booking", "Thời lượng mặc định phải nằm trong khoảng tối thiểu/tối đa.");
  if (settings.bookingPolicy.holdMinutes <= 0) add("booking", "Thời gian giữ bàn phải lớn hơn 0.");
  if (settings.bookingPolicy.advanceBookingDays <= 0) add("booking", "Số ngày đặt trước phải lớn hơn 0.");
  addMediaIssue(issues, "seo", settings.seo.ogImageUrl || "", "image");
  if (settings.seo.canonicalUrl && !/^https?:\/\//i.test(settings.seo.canonicalUrl)) add("seo", "Canonical URL phải bắt đầu bằng http:// hoặc https://.");
  if (!/^#[0-9a-fA-F]{6}$/.test(settings.theme.primaryColor)) add("legal-theme", "Màu chính phải là mã hex 6 ký tự.");
  if (!/^#[0-9a-fA-F]{6}$/.test(settings.theme.accentColor)) add("legal-theme", "Màu nhấn phải là mã hex 6 ký tự.");
  addMediaIssue(issues, "legal-theme", settings.qrCode.imageUrl || "", "image");
  return issues;
}

function addMediaIssue(issues: ValidationIssue[], section: SectionId, value: string, mediaType: "image" | "video" | "all", required = false) {
  const message = validateMediaValue(value, mediaType, required);
  if (message) issues.push({ section, message });
}

function addLinkIssue(issues: ValidationIssue[], section: SectionId, type: LinkType, value: string) {
  const message = validateLink(type, value);
  if (message) issues.push({ section, message });
}

function groupIssues(issues: ValidationIssue[]) {
  return issues.reduce((map, issue) => {
    map[issue.section] = [...(map[issue.section] || []), issue];
    return map;
  }, {} as Partial<Record<SectionId, ValidationIssue[]>>);
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

function BadgeEditor({ badges, onChange }: { badges: string[]; onChange: (badges: string[]) => void }) {
  function update(index: number, value: string) {
    onChange(badges.map((badge, itemIndex) => itemIndex === index ? value : badge));
  }
  return (
    <div className="settings-repeater full-field">
      <div className="repeater-head"><h3>Badge thống kê</h3><button type="button" className="primary-btn" onClick={() => onChange([...badges, "Giá trị mới"]) }>Thêm badge</button></div>
      {badges.map((badge, index) => (
        <div className="media-input-row" key={index}>
          <input aria-label={`Badge ${index + 1}`} value={badge} onChange={(event) => update(index, event.target.value)} />
          <button type="button" className="danger-btn" onClick={() => onChange(badges.filter((_, itemIndex) => itemIndex !== index))}>Xóa</button>
        </div>
      ))}
    </div>
  );
}

function Repeater<T>({ title, section, name, items, children, onAdd, onRemove }: {
  title: string;
  section: SectionId;
  name: ListName;
  items: T[];
  children: (item: T, index: number) => React.ReactNode;
  onAdd: (section: SectionId, name: ListName) => void;
  onRemove: (section: SectionId, name: ListName, index: number) => void;
}) {
  return (
    <div className="settings-repeater">
      <div className="repeater-head"><h2>{title}</h2><button type="button" className="primary-btn" onClick={() => onAdd(section, name)}>Thêm mục</button></div>
      {items.map((item, index) => (
        <div className="repeater-item" key={index}>
          <div className="repeater-item-head"><strong>{sectionLabels[section]} {index + 1}</strong><button type="button" className="danger-btn" onClick={() => onRemove(section, name, index)}>Xóa</button></div>
          {children(item, index)}
        </div>
      ))}
    </div>
  );
}
