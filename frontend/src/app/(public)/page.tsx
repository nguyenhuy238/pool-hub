import type { Metadata } from "next";
import { LandingPage } from "@/components/landing/LandingPage";
import { defaultLandingSettings } from "@/lib/api/landingSettingsApi";

export const metadata: Metadata = {
  title: defaultLandingSettings.seo.metaTitle,
  description: defaultLandingSettings.seo.metaDescription,
  keywords: defaultLandingSettings.seo.metaKeywords,
  alternates: defaultLandingSettings.seo.canonicalUrl ? { canonical: defaultLandingSettings.seo.canonicalUrl } : undefined,
  icons: defaultLandingSettings.generalInfo.faviconUrl ? { icon: defaultLandingSettings.generalInfo.faviconUrl } : undefined,
  openGraph: {
    title: defaultLandingSettings.seo.metaTitle,
    description: defaultLandingSettings.seo.metaDescription,
    images: defaultLandingSettings.seo.ogImageUrl ? [defaultLandingSettings.seo.ogImageUrl] : undefined
  }
};

export default function HomePage() {
  return <LandingPage />;
}
