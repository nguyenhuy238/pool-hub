import { uspItems } from "@/lib/mock/landingData";

export function USPSection() {
  return (
    <section className="landing-section">
      <div className="section-heading">
        <p className="eyebrow">Lý do chọn PoolHub</p>
        <h2>Mọi chi tiết được tối ưu cho một buổi chơi trọn vẹn</h2>
      </div>
      <div className="landing-card-grid">
        {uspItems.map((item) => (
          <article className="landing-card" key={item.title}>
            <div className="card-icon" aria-hidden="true">+</div>
            <h3>{item.title}</h3>
            <p>{item.text}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
