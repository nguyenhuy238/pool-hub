import type { GeneralInfoSettings } from "@/lib/api/landingSettingsApi";

export function ContactSection({ info }: { info: GeneralInfoSettings }) {
  const directionUrl = info.googleMapsDirectionUrl || info.googleMapsShareUrl || info.googleMapsUrl || "https://www.google.com/maps";
  return (
    <section className="landing-section contact-section" id="contact">
      <div>
        <p className="eyebrow">Liên hệ & vị trí</p>
        <h2>{info.centerName}</h2>
        <p>Hotline: <a href={`tel:${info.hotline.replace(/\s/g, "")}`}>{info.hotline}</a></p>
        <p>Email: <a href={`mailto:${info.email}`}>{info.email}</a></p>
        <p>Địa chỉ: {info.address}</p>
        <p>Giờ mở cửa: {info.openingHours}</p>
        <div className="hero-actions">
          <a className="primary-btn" href={`tel:${info.hotline.replace(/\s/g, "")}`}>Gọi nhanh</a>
          <a className="secondary-btn" href={directionUrl} target="_blank" rel="noreferrer">Chỉ đường</a>
        </div>
      </div>
      {info.mapDisplayMode === "hidden" ? null : info.mapDisplayMode === "embed" && info.googleMapsEmbedUrl ? (
        <div className="contact-map"><iframe title={`Bản đồ ${info.centerName}`} src={info.googleMapsEmbedUrl} loading="lazy" referrerPolicy="no-referrer-when-downgrade" /></div>
      ) : info.mapDisplayMode === "external" ? (
        <a className="map-placeholder" href={directionUrl} target="_blank" rel="noreferrer"><span>Mở Google Maps</span></a>
      ) : (
        <div className="map-placeholder" aria-label="Bản đồ PoolHub"><span>{info.address}</span></div>
      )}
    </section>
  );
}
