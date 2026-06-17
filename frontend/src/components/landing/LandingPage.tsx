import { AvailabilitySection } from "@/components/landing/AvailabilitySection";
import { BookingForm } from "@/components/landing/BookingForm";
import { ContactSection } from "@/components/landing/ContactSection";
import { GallerySection } from "@/components/landing/GallerySection";
import { HeroSection } from "@/components/landing/HeroSection";
import { LandingFooter } from "@/components/landing/LandingFooter";
import { PricingSection } from "@/components/landing/PricingSection";
import { ReviewSection } from "@/components/landing/ReviewSection";
import { ServicesSection } from "@/components/landing/ServicesSection";
import { USPSection } from "@/components/landing/USPSection";

export function LandingPage() {
  return (
    <>
      <HeroSection />
      <USPSection />
      <ServicesSection />
      <PricingSection />
      <AvailabilitySection />
      <BookingForm />
      <ReviewSection />
      <GallerySection />
      <ContactSection />
      <LandingFooter />
      <a className="mobile-sticky-cta" href="#booking">Đặt bàn</a>
    </>
  );
}
