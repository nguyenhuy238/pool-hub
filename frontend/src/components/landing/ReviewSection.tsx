import { activeSorted, type ReviewSettings } from "@/lib/api/landingSettingsApi";
import { SafeImage } from "@/components/landing/SafeImage";

export function ReviewSection({ items }: { items: ReviewSettings[] }) {
  const visibleItems = activeSorted(items).filter((item) => item.isFeatured);
  return (
    <section className="landing-section" id="reviews">
      <div className="section-heading">
        <p className="eyebrow">Đánh giá khách hàng</p>
        <h2>Phản hồi từ những nhóm đã đặt bàn tại PoolHub</h2>
      </div>
      <div className="landing-card-grid">
        {visibleItems.map((review) => (
          <article className="review-card" key={review.customerName}>
            <div className="review-head">
              {review.avatarUrl ? <SafeImage className="avatar-image" src={review.avatarUrl} alt={`Avatar ${review.customerName}`} /> : <span className="avatar">{review.customerName.slice(0, 2).toUpperCase()}</span>}
              <div><strong>{review.customerName}</strong><p>{"*".repeat(review.rating)}</p></div>
            </div>
            <p>{review.content}</p>
            {review.checkInImageUrl ? <SafeImage className="review-checkin" src={review.checkInImageUrl} alt={`Ảnh check-in của ${review.customerName}`} /> : null}
          </article>
        ))}
      </div>
    </section>
  );
}
