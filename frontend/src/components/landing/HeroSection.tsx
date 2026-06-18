"use client";

import { useState } from "react";
import type { HeroSettings } from "@/lib/api/landingSettingsApi";

export function HeroSection({ hero }: { hero: HeroSettings }) {
  const [videoFailed, setVideoFailed] = useState(false);
  const showVideo = hero.useVideo && hero.backgroundVideoUrl && !videoFailed;
  return (
    <section className="landing-hero" id="hero" style={{ backgroundImage: `url("${hero.fallbackImageUrl || hero.backgroundImageUrl}")` }}>
      {showVideo ? (
        <video className="hero-video" autoPlay muted loop playsInline poster={hero.fallbackImageUrl || hero.backgroundImageUrl}>
          <source src={hero.backgroundVideoUrl} type={hero.backgroundVideoUrl?.toLowerCase().includes(".webm") ? "video/webm" : "video/mp4"} onError={() => setVideoFailed(true)} />
        </video>
      ) : null}
      <div className="hero-overlay" />
      <div className="landing-hero-content">
        <p className="eyebrow">{hero.subtitle}</p>
        <h1>{hero.title}</h1>
        <p>{hero.description}</p>
        <div className="hero-actions">
          <a className="primary-btn hero-cta" href={hero.primaryCtaLink || "#booking"} target={hero.primaryCtaLinkType === "external" || hero.primaryCtaLinkType === "map" ? "_blank" : undefined} rel={hero.primaryCtaLinkType === "external" || hero.primaryCtaLinkType === "map" ? "noreferrer" : undefined}>{hero.primaryCtaText}</a>
          <a className="secondary-btn" href={hero.secondaryCtaLink || "#pricing"} target={hero.secondaryCtaLinkType === "external" || hero.secondaryCtaLinkType === "map" ? "_blank" : undefined} rel={hero.secondaryCtaLinkType === "external" || hero.secondaryCtaLinkType === "map" ? "noreferrer" : undefined}>{hero.secondaryCtaText}</a>
        </div>
        <div className="hero-stats" aria-label="Thống kê nhanh PoolHub">
          {hero.badges.map((badge) => {
            const [strong, ...rest] = badge.split(" ");
            return <span key={badge}><strong>{strong}</strong> {rest.join(" ")}</span>;
          })}
        </div>
      </div>
    </section>
  );
}
