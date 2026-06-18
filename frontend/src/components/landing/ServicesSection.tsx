import { activeSorted, type ServiceSettings } from "@/lib/api/landingSettingsApi";
import { SafeImage } from "@/components/landing/SafeImage";

export function ServicesSection({ items }: { items: ServiceSettings[] }) {
  const visibleItems = activeSorted(items);
  return (
    <section className="landing-section muted-band" id="services">
      <div className="section-heading">
        <p className="eyebrow">Dịch vụ & menu</p>
        <h2>Từ trận đấu nghiêm túc đến buổi hẹn cuối ngày</h2>
      </div>
      <div className="service-grid">
        {visibleItems.map((item) => (
          <article className="service-card" key={item.title}>
            <SafeImage src={item.imageUrl} alt={`${item.title} tại PoolHub`} />
            <div>
              <h3>{item.title}</h3>
              <p>{item.description}</p>
              <strong>{item.priceText}</strong>
              {item.ctaText && item.ctaLink ? <a className="service-link" href={item.ctaLink} target={item.ctaLinkType === "external" || item.ctaLinkType === "map" ? "_blank" : undefined} rel={item.ctaLinkType === "external" || item.ctaLinkType === "map" ? "noreferrer" : undefined}>{item.ctaText}</a> : null}
            </div>
          </article>
        ))}
      </div>
      <p className="data-note">Menu nổi bật đang dùng dữ liệu mẫu và đã tách sẵn để thay bằng API sản phẩm public.</p>
    </section>
  );
}
