export function ContactSection() {
  return (
    <section className="landing-section contact-section" id="contact">
      <div>
        <p className="eyebrow">Liên hệ & vị trí</p>
        <h2>PoolHub Center</h2>
        <p>Hotline: <a href="tel:0901234567">0901 234 567</a></p>
        <p>Địa chỉ: 123 Nguyễn Trãi, Quận 1, TP. Hồ Chí Minh</p>
        <p>Giờ mở cửa: 09:00 - 24:00 hằng ngày</p>
        <div className="hero-actions">
          <a className="primary-btn" href="tel:0901234567">Gọi nhanh</a>
          <a className="secondary-btn" href="https://www.google.com/maps" target="_blank" rel="noreferrer">Chỉ đường</a>
        </div>
      </div>
      <div className="map-placeholder" aria-label="Bản đồ PoolHub">
        <span>Google Maps Embed</span>
      </div>
    </section>
  );
}
