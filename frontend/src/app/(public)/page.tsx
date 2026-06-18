import type { Metadata } from "next";
import { LandingPage } from "@/components/landing/LandingPage";
import { defaultLandingSettings, type LandingPageSettings } from "@/lib/api/landingSettingsApi";

export async function generateMetadata(): Promise<Metadata> {
  const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";
  try {
    const response = await fetch(`${apiBaseUrl}/api/public/landing-page`, { next: { revalidate: 60 } });
    if (!response.ok) throw new Error("Landing settings API failed");
    const payload = await response.json() as { data?: LandingPageSettings };
    const settings = payload.data || defaultLandingSettings;
    return {
      title: settings.seo.metaTitle,
      description: settings.seo.metaDescription,
      keywords: settings.seo.metaKeywords,
      alternates: settings.seo.canonicalUrl ? { canonical: settings.seo.canonicalUrl } : undefined,
      icons: settings.generalInfo.faviconUrl ? { icon: settings.generalInfo.faviconUrl } : undefined,
      openGraph: {
        title: settings.seo.metaTitle,
        description: settings.seo.metaDescription,
        images: settings.seo.ogImageUrl ? [settings.seo.ogImageUrl] : undefined
      }
    };
  } catch {
    return {
      title: defaultLandingSettings.seo.metaTitle,
      description: defaultLandingSettings.seo.metaDescription
    };
  }
}

export default function HomePage() {
  return <LandingPage />;
}
