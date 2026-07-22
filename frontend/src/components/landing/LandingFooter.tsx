import { activeSorted, type GeneralInfoSettings, type BookingPolicySettings, type FooterSettings, type LegalSettings, type QrCodeSettings, type SocialLinkSettings } from "@/lib/api/landingSettingsApi";
import { SafeImage } from "@/components/landing/SafeImage";

export function LandingFooter({ info, bookingPolicy, socialLinks, footer, legal, qrCode }: { info: GeneralInfoSettings; bookingPolicy: BookingPolicySettings; socialLinks: SocialLinkSettings[]; footer: FooterSettings; legal: LegalSettings; qrCode: QrCodeSettings }) {
  const visibleSocials = activeSorted(socialLinks || []);
  return (
    <footer className="landing-footer">
      <div>
        <a className="brand" href="/"><span>PH</span>{info.centerName}</a>
        <p>{info.shortDescription}</p>
        <div className="social-links">
          {visibleSocials.map((item) => <a key={`${item.platform}-${item.displayOrder}`} href={item.url} target="_blank" rel="noreferrer" aria-label={item.platform}>{item.icon || item.platform}</a>)}
        </div>
      </div>
      <div>
        <strong>{footer.menuTitle}</strong>
        <a href="#services">Dịch vụ</a>
        <a href="#pricing">Bảng giá</a>
        <a href="#booking">Đặt bàn</a>
      </div>
      <div>
        <strong>{footer.policyTitle}</strong>
        <p>{bookingPolicy.policyNote}</p>
        {legal.privacyPolicy ? <p>{legal.privacyPolicy}</p> : null}
        {legal.termsOfService ? <p>{legal.termsOfService}</p> : null}
        {qrCode.isEnabled && qrCode.imageUrl ? <SafeImage className="footer-qr" src={qrCode.imageUrl} alt={qrCode.caption || "PoolHub QR"} /> : null}
        <p>{footer.copyright}</p>
      </div>
    </footer>
  );
}
