"use client";
/* eslint-disable @next/next/no-img-element */

import Link from "next/link";
import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { PortalLoginForm } from "@/components/auth/PortalLoginForm";
import { defaultLandingSettings, landingSettingsApi } from "@/lib/api/landingSettingsApi";

export function PublicHeader() {
  const searchParams = useSearchParams();
  const [bannerVisible, setBannerVisible] = useState(true);
  const [menuOpen, setMenuOpen] = useState(false);
  const [loginOpen, setLoginOpen] = useState(false);
  const [settings, setSettings] = useState(defaultLandingSettings);
  const banner = settings.promotionBanner;

  useEffect(() => {
    landingSettingsApi.public().then(setSettings).catch(() => setSettings(defaultLandingSettings));
  }, []);

  useEffect(() => {
    if (searchParams?.get("login") !== "customer") return;

    setLoginOpen(true);
    const url = new URL(window.location.href);
    url.searchParams.delete("login");
    window.history.replaceState(window.history.state, "", `${url.pathname}${url.search}${url.hash}`);
  }, [searchParams]);

  const closeMenu = () => setMenuOpen(false);

  return (
    <>
      {bannerVisible && banner.isEnabled ? (
        <div className="promo-banner">
          <span>{banner.content}</span>
          <a href={banner.ctaLink || "#booking"} target={banner.ctaLinkType === "external" || banner.ctaLinkType === "map" ? "_blank" : undefined} rel={banner.ctaLinkType === "external" || banner.ctaLinkType === "map" ? "noreferrer" : undefined} onClick={closeMenu}>{banner.ctaText}</a>
          <button type="button" aria-label="Đóng thông báo" onClick={() => setBannerVisible(false)}>x</button>
        </div>
      ) : null}
      <header className="public-nav">
        <Link className="brand" href="/" onClick={closeMenu}>{settings.generalInfo.logoUrl ? <img className="brand-logo" src={settings.generalInfo.logoUrl} alt={settings.generalInfo.centerName} onError={(event) => { event.currentTarget.style.display = "none"; }} /> : <span>PH</span>}{settings.generalInfo.centerName || "PoolHub"}</Link>
        <button className="hamburger" type="button" aria-label="Mở menu" onClick={() => setMenuOpen((value) => !value)}>
          <span />
          <span />
          <span />
        </button>
        <nav className={menuOpen ? "public-menu open" : "public-menu"}>
          <a href="#hero" onClick={closeMenu}>Trang chủ</a>
          <a href="#services" onClick={closeMenu}>Dịch vụ</a>
          <a href="#pricing" onClick={closeMenu}>Bảng giá</a>
          <a href="#booking" onClick={closeMenu}>Đặt bàn</a>
          <a href="#reviews" onClick={closeMenu}>Đánh giá</a>
          <a href="#contact" onClick={closeMenu}>Liên hệ</a>
          <a className="primary-btn" href="#booking" onClick={closeMenu}>Đặt bàn ngay</a>
          <button
            className="ghost-btn"
            type="button"
            aria-haspopup="dialog"
            aria-expanded={loginOpen}
            onClick={() => {
              closeMenu();
              setLoginOpen(true);
            }}
          >
            Đăng nhập khách hàng
          </button>
        </nav>
      </header>
      {loginOpen ? (
        <PortalLoginForm
          portal="customer"
          presentation="modal"
          onClose={() => setLoginOpen(false)}
        />
      ) : null}
    </>
  );
}
