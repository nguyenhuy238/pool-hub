"use client";

import { type CSSProperties, useEffect, useState } from "react";
import { BookingWizard } from "@/components/landing/BookingWizard";
import { ContactSection } from "@/components/landing/ContactSection";
import { GallerySection } from "@/components/landing/GallerySection";
import { HeroSection } from "@/components/landing/HeroSection";
import { LandingFooter } from "@/components/landing/LandingFooter";
import { PricingSection } from "@/components/landing/PricingSection";
import { ReviewSection } from "@/components/landing/ReviewSection";
import { ServicesSection } from "@/components/landing/ServicesSection";
import { USPSection } from "@/components/landing/USPSection";
import { AboutSection } from "@/components/landing/AboutSection";
import { defaultLandingSettings, landingSettingsApi, type LandingPageSettings } from "@/lib/api/landingSettingsApi";

export function LandingPage() {
  const [settings, setSettings] = useState<LandingPageSettings>(defaultLandingSettings);

  useEffect(() => {
    landingSettingsApi.public().then(setSettings).catch((error) => {
      console.error("Failed to load landing page settings", error);
      setSettings(defaultLandingSettings);
    });
  }, []);

  const themeStyle = {
    "--brand": settings.theme.primaryColor,
    "--accent": settings.theme.accentColor
  } as CSSProperties;

  return (
    <div style={themeStyle}>
      <HeroSection hero={settings.hero} />
      <AboutSection value={settings.about} />
      <USPSection items={settings.uspItems} />
      <ServicesSection items={settings.services} />
      <PricingSection />
      <BookingWizard policy={settings.bookingPolicy} />
      <ReviewSection items={settings.reviews} />
      <GallerySection items={settings.gallery} />
      <ContactSection info={settings.generalInfo} />
      <LandingFooter info={settings.generalInfo} bookingPolicy={settings.bookingPolicy} socialLinks={settings.socialLinks || []} footer={settings.footer} legal={settings.legal} qrCode={settings.qrCode} />
      <a className="mobile-sticky-cta" href="#booking">Đặt bàn</a>
    </div>
  );
}
