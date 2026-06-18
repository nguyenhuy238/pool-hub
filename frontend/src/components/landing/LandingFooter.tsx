import { activeSorted, type GeneralInfoSettings, type BookingPolicySettings, type SocialLinkSettings } from "@/lib/api/landingSettingsApi";

export function LandingFooter({ info, bookingPolicy, socialLinks }: { info: GeneralInfoSettings; bookingPolicy: BookingPolicySettings; socialLinks: SocialLinkSettings[] }) {
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
        <strong>Menu nhanh</strong>
        <a href="#services">Dịch vụ</a>
        <a href="#pricing">Bảng giá</a>
        <a href="#booking">Đặt bàn</a>
      </div>
      <div>
        <strong>Chính sách đặt lịch</strong>
        <p>{bookingPolicy.policyNote}</p>
        <p>Copyright 2026 PoolHub.</p>
      </div>
    </footer>
  );
}
