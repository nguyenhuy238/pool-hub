import Image from "next/image";
import { serviceItems } from "@/lib/mock/landingData";

export function ServicesSection() {
  return (
    <section className="landing-section muted-band" id="services">
      <div className="section-heading">
        <p className="eyebrow">Dịch vụ & menu</p>
        <h2>Từ trận đấu nghiêm túc đến buổi hẹn cuối ngày</h2>
      </div>
      <div className="service-grid">
        {serviceItems.map((item) => (
          <article className="service-card" key={item.title}>
            <Image src={item.image} alt={`${item.title} tại PoolHub`} width={720} height={420} loading="lazy" />
            <div>
              <h3>{item.title}</h3>
              <p>{item.description}</p>
              <strong>{item.price}</strong>
            </div>
          </article>
        ))}
      </div>
      <p className="data-note">Menu nổi bật đang dùng dữ liệu mẫu và đã tách sẵn để thay bằng API sản phẩm public.</p>
    </section>
  );
}
