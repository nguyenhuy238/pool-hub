import { reviews } from "@/lib/mock/landingData";

export function ReviewSection() {
  return (
    <section className="landing-section" id="reviews">
      <div className="section-heading">
        <p className="eyebrow">Đánh giá khách hàng</p>
        <h2>Phản hồi từ những nhóm đã đặt bàn tại PoolHub</h2>
      </div>
      <div className="landing-card-grid">
        {reviews.map((review) => (
          <article className="review-card" key={review.name}>
            <div className="review-head">
              <span className="avatar">{review.avatar}</span>
              <div><strong>{review.name}</strong><p>{"*".repeat(review.rating)}</p></div>
            </div>
            <p>{review.content}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
