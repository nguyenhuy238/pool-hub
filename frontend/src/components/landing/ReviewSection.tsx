"use client";

import { FormEvent, useState } from "react";
import type { ReviewSettings } from "@/lib/api/landingSettingsApi";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import { SafeImage } from "@/components/landing/SafeImage";
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

export function ReviewSection({ items }: { items: ReviewItem[] }) {
  const visibleItems = items.map(normalizeReview);
  const [form, setForm] = useState({ referenceType: "bookingCode", referenceCode: "", fullName: "", phoneNumber: "", rating: 5, content: "" });
  const [status, setStatus] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setStatus(null);
    setSubmitting(true);
    try {
      await customerReviewsApi.publicCreate({
        phoneNumber: form.phoneNumber,
        fullName: form.fullName,
        rating: Number(form.rating),
        content: form.content,
        bookingCode: form.referenceType === "bookingCode" ? form.referenceCode : undefined,
        sessionCode: form.referenceType === "sessionCode" ? form.referenceCode : undefined,
        invoiceCode: form.referenceType === "invoiceCode" ? form.referenceCode : undefined
      });
      setForm({ referenceType: "bookingCode", referenceCode: "", fullName: "", phoneNumber: "", rating: 5, content: "" });
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
      </div>
      <div className="landing-card-grid">
        {visibleItems.map((review) => (
          <article className="review-card" key={review.publicId}>
            <div className="review-head">
              {review.avatarUrl ? <SafeImage className="avatar-image" src={review.avatarUrl} alt={`Avatar ${review.displayName}`} /> : <span className="avatar">{review.displayName.slice(0, 2).toUpperCase()}</span>}
              <div><strong>{review.displayName}</strong><p>{"*".repeat(review.rating)}</p></div>
            </div>
            <p>{review.content}</p>
            {review.checkInImageUrl ? <SafeImage className="review-checkin" src={review.checkInImageUrl} alt={`Ảnh check-in của ${review.displayName}`} /> : null}
          </article>
        ))}
        {!visibleItems.length ? <div className="state-card">Chưa có đánh giá được duyệt.</div> : null}
      </div>
      <form className="card form-grid" onSubmit={submit} style={{ marginTop: 24 }}>
        <h3 style={{ gridColumn: "1 / -1", margin: 0 }}>Gửi đánh giá</h3>
        <label><span>Loại mã</span><select value={form.referenceType} onChange={(event) => setForm({ ...form, referenceType: event.target.value })}><option value="bookingCode">Booking code</option><option value="sessionCode">Session code</option><option value="invoiceCode">Invoice code</option></select></label>
        <label><span>Mã xác nhận</span><input value={form.referenceCode} onChange={(event) => setForm({ ...form, referenceCode: event.target.value })} /></label>
        <label><span>Số điện thoại</span><input required inputMode="tel" value={form.phoneNumber} onChange={(event) => setForm({ ...form, phoneNumber: event.target.value })} /></label>
        <label><span>Tên hiển thị</span><input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} /></label>
        <label><span>Số sao</span><select value={form.rating} onChange={(event) => setForm({ ...form, rating: Number(event.target.value) })}><option value={5}>5</option><option value={4}>4</option><option value={3}>3</option><option value={2}>2</option><option value={1}>1</option></select></label>
        <label className="full-field"><span>Nội dung</span><textarea required rows={3} maxLength={1000} value={form.content} onChange={(event) => setForm({ ...form, content: event.target.value })} /></label>
        {status ? <p className="state-card" style={{ gridColumn: "1 / -1" }}>{status}</p> : null}
        <div style={{ gridColumn: "1 / -1", display: "flex", justifyContent: "flex-end" }}><button className="primary-btn" disabled={submitting}>{submitting ? "Đang gửi..." : "Gửi đánh giá"}</button></div>
      </form>
    </section>
  );
}
