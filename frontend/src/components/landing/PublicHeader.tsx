"use client";

import Link from "next/link";
import { useState } from "react";
import { promotionMessages } from "@/lib/mock/landingData";

export function PublicHeader() {
  const [bannerVisible, setBannerVisible] = useState(true);
  const [menuOpen, setMenuOpen] = useState(false);

  const closeMenu = () => setMenuOpen(false);

  return (
    <>
      {bannerVisible ? (
        <div className="promo-banner">
          <span>{promotionMessages.join(" | ")}</span>
          <a href="#booking" onClick={closeMenu}>Đặt ngay</a>
          <button type="button" aria-label="Đóng thông báo" onClick={() => setBannerVisible(false)}>x</button>
        </div>
      ) : null}
      <header className="public-nav">
        <Link className="brand" href="/" onClick={closeMenu}><span>PH</span>PoolHub</Link>
        <button className="hamburger" type="button" aria-label="Mở menu" onClick={() => setMenuOpen((value) => !value)}>
          <span />
          <span />
          <span />
        </button>
        <nav className={menuOpen ? "public-menu open" : "public-menu"}>
          <a href="#home" onClick={closeMenu}>Trang chủ</a>
          <a href="#services" onClick={closeMenu}>Dịch vụ</a>
          <a href="#pricing" onClick={closeMenu}>Bảng giá</a>
          <a href="#booking" onClick={closeMenu}>Đặt bàn</a>
          <a href="#reviews" onClick={closeMenu}>Đánh giá</a>
          <a href="#contact" onClick={closeMenu}>Liên hệ</a>
          <a className="primary-btn" href="#booking" onClick={closeMenu}>Đặt bàn ngay</a>
          <Link className="ghost-btn" href="/login" onClick={closeMenu}>Đăng nhập nhân viên</Link>
        </nav>
      </header>
    </>
  );
}
