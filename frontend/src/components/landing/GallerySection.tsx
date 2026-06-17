import Image from "next/image";
import { galleryImages } from "@/lib/mock/landingData";

export function GallerySection() {
  return (
    <section className="landing-section muted-band">
      <div className="section-heading">
        <p className="eyebrow">Không gian thực tế</p>
        <h2>Hình ảnh sẵn sàng thay bằng ảnh check-in thật của quán</h2>
      </div>
      <div className="gallery-grid">
        {galleryImages.map((image) => <Image key={image.alt} src={image.src} alt={image.alt} width={640} height={420} loading="lazy" />)}
      </div>
    </section>
  );
}
