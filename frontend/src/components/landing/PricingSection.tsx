import { activeSorted, type PricingHighlightSettings } from "@/lib/api/landingSettingsApi";

export function PricingSection({ items }: { items: PricingHighlightSettings[] }) {
  const visibleItems = activeSorted(items);
  return (
    <section className="landing-section" id="pricing">
      <div className="section-heading split-heading">
        <div>
          <p className="eyebrow">Bảng giá ưu đãi</p>
          <h2>Giá theo khung giờ, để nhóm bạn chủ động ngân sách</h2>
        </div>
        <div className="golden-hour">Thứ 2 - Thứ 6 trước 17h: ưu đãi tốt hơn</div>
      </div>
      <div className="pricing-grid">
        {visibleItems.map((item) => (
          <article className="price-card" key={`${item.title}-${item.displayOrder}`}>
            <span>{item.badge || item.tableType || "Khung giá"}</span>
            <h3>{item.priceText}</h3>
            <p>{item.description || item.timeRange}</p>
          </article>
        ))}
      </div>
      <p className="data-note">Bảng giá này là nội dung marketing. Logic tính tiền thật vẫn nằm trong module Pricing.</p>
    </section>
  );
}
