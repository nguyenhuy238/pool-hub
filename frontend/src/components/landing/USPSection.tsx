import { activeSorted, type UspSettings } from "@/lib/api/landingSettingsApi";

export function USPSection({ items }: { items: UspSettings[] }) {
  const visibleItems = activeSorted(items);
  return (
    <section className="landing-section">
      <div className="section-heading">
        <p className="eyebrow">Lý do chọn PoolHub</p>
        <h2>Mọi chi tiết được tối ưu cho một buổi chơi trọn vẹn</h2>
      </div>
      <div className="landing-card-grid">
        {visibleItems.map((item) => (
          <article className="landing-card" key={item.title}>
            <div className="card-icon" aria-hidden="true">{item.icon || "+"}</div>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
