export function HeroSection() {
  return (
    <section className="landing-hero" id="home">
      <video className="hero-video" autoPlay muted loop playsInline poster="/images/poolhub/hero.png">
      </video>
      <div className="hero-overlay" />
      <div className="landing-hero-content">
        <p className="eyebrow">PoolHub Billiards & Entertainment</p>
        <h1>Đặt bàn bi-a nhanh chóng - Trải nghiệm giải trí đẳng cấp</h1>
        <p>Không gian hiện đại, bàn chuẩn, đồ uống phục vụ tận bàn và đặt lịch online để nhóm bạn đến là có bàn chơi.</p>
        <div className="hero-actions">
          <a className="primary-btn hero-cta" href="#booking">Đặt bàn ngay</a>
          <a className="secondary-btn" href="#pricing">Xem bảng giá</a>
        </div>
        <div className="hero-stats" aria-label="Thống kê nhanh PoolHub">
          <span><strong>12+</strong> bàn sẵn sàng</span>
          <span><strong>09:00</strong> mở cửa mỗi ngày</span>
          <span><strong>4.8/5</strong> đánh giá khách</span>
        </div>
      </div>
    </section>
  );
}
