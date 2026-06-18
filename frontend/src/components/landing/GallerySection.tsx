import { activeSorted, type GallerySettings } from "@/lib/api/landingSettingsApi";
import { SafeImage } from "@/components/landing/SafeImage";

export function GallerySection({ items }: { items: GallerySettings[] }) {
  const visibleItems = activeSorted(items);
  return (
    <section className="landing-section muted-band" id="gallery">
      <div className="section-heading">
        <p className="eyebrow">Không gian thực tế</p>
        <h2>Hình ảnh sẵn sàng thay bằng ảnh check-in thật của quán</h2>
      </div>
      <div className="gallery-grid">
        {visibleItems.map((image) => <SafeImage key={`${image.imageUrl}-${image.displayOrder}`} src={image.imageUrl} alt={image.altText || image.title} />)}
      </div>
    </section>
  );
}
