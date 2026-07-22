"use client";

import { FormEvent, useState } from "react";
import type { ReviewSettings } from "@/lib/api/landingSettingsApi";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import { SafeImage } from "@/components/landing/SafeImage";
import { StarRatingInput } from "@/components/reviews/StarRatingInput";
import type { PublicReview } from "@/types";

type ReviewItem = PublicReview | ReviewSettings;

function normalizeReview(item: ReviewItem): PublicReview {
  if ("displayName" in item) return item;
  return {
    publicId: item.customerName,
    displayName: item.customerName,
    rating: item.rating,
    content: item.content,
    avatarUrl: item.avatarUrl,
    checkInImageUrl: item.checkInImageUrl,
    isFeatured: item.isFeatured,
    displayOrder: item.displayOrder
  };
}

export function ReviewSection({ items, summary }: { items: ReviewItem[]; summary?: { averageRating?: number; totalItems?: number; ratingDistribution?: Record<number, number> } }) {
  const visibleItems = items.map(normalizeReview);
  const [formOpen, setFormOpen] = useState(false);
  const [form, setForm] = useState({ referenceCode: "", rating: 0, content: "", isAnonymous: false });
  const [status, setStatus] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const averageRating = summary?.averageRating ?? (visibleItems.length ? visibleItems.reduce((sum, item) => sum + item.rating, 0) / visibleItems.length : 0);
  const totalReviews = summary?.totalItems ?? visibleItems.length;

  async function submit(event: FormEvent) {
    event.preventDefault();
    setStatus(null);
    if (!form.referenceCode.trim()) {
      setStatus("Vui lòng nhập mã hóa đơn, phiên chơi hoặc booking.");
      return;
    }
    if (!form.rating) {
      setStatus("Vui lòng chọn số sao đánh giá.");
      return;
    }
    setSubmitting(true);
    try {
      await customerReviewsApi.publicCreate({
        rating: Number(form.rating),
        content: form.content.trim() || undefined,
        referenceCode: form.referenceCode.trim(),
        isAnonymous: form.isAnonymous
      });
      setForm({ referenceCode: "", rating: 0, content: "", isAnonymous: false });
      setStatus("Cảm ơn bạn đã chia sẻ trải nghiệm tại PoolHub.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Không gửi được đánh giá.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <section className="landing-section" id="reviews">
      <div className="section-heading">
        <p className="eyebrow">Đánh giá khách hàng</p>
        <h2>Phản hồi từ những nhóm đã đặt bàn tại PoolHub</h2>
        <p>{averageRating ? `${averageRating.toFixed(1)}/5 từ ${totalReviews} đánh giá` : "Chưa có điểm trung bình"}</p>
      </div>
      <div className="landing-card-grid">
        {visibleItems.map((review) => (
          <article className="review-card" key={review.publicId}>
            <div className="review-head">
              {review.avatarUrl ? <SafeImage className="avatar-image" src={review.avatarUrl} alt={`Avatar ${review.displayName}`} /> : <span className="avatar">{review.displayName.slice(0, 2).toUpperCase()}</span>}
              <div><strong>{review.displayName}</strong><p>{"*".repeat(review.rating)} {review.isVerified ? <span className="badge green">Đã xác thực</span> : null}</p></div>
            </div>
            <p>{review.content}</p>
            {review.checkInImageUrl ? <SafeImage className="review-checkin" src={review.checkInImageUrl} alt={`Ảnh check-in của ${review.displayName}`} /> : null}
          </article>
        ))}
        {!visibleItems.length ? <div className="state-card">Chưa có đánh giá được duyệt.</div> : null}
      </div>
      <div style={{ marginTop: 24, display: "flex", justifyContent: "center" }}>
        <button type="button" className="secondary-btn" onClick={() => setFormOpen((value) => !value)}>Bạn đã chơi tại PoolHub? Gửi đánh giá</button>
      </div>
      {formOpen ? (
        <form className="card form-grid" onSubmit={submit} style={{ marginTop: 16 }}>
          <h3 style={{ gridColumn: "1 / -1", margin: 0 }}>Gửi đánh giá</h3>
          <label className="full-field"><span>Mã hóa đơn, phiên chơi hoặc booking</span><input value={form.referenceCode} onChange={(event) => setForm({ ...form, referenceCode: event.target.value })} /></label>
          <label className="full-field"><span>Số sao</span><StarRatingInput value={form.rating} onChange={(rating) => setForm({ ...form, rating })} disabled={submitting} /></label>
          <label className="full-field"><span>Nội dung góp ý</span><textarea rows={3} maxLength={1000} value={form.content} onChange={(event) => setForm({ ...form, content: event.target.value })} /></label>
          <label className="check-row full-field"><input type="checkbox" checked={form.isAnonymous} onChange={(event) => setForm({ ...form, isAnonymous: event.target.checked })} /><span>Đăng ẩn danh</span></label>
          {status ? <p className="state-card" style={{ gridColumn: "1 / -1" }}>{status}</p> : null}
          <div style={{ gridColumn: "1 / -1", display: "flex", justifyContent: "flex-end" }}><button className="primary-btn" disabled={submitting || !form.rating}>{submitting ? "Đang gửi..." : "Gửi đánh giá"}</button></div>
        </form>
      ) : null}
    </section>
  );
}
