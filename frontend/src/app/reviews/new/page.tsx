"use client";

import { FormEvent, Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { customerReviewsApi } from "@/lib/api/customerReviewsApi";
import type { ReviewInvitation } from "@/types";

export default function NewReviewPage() {
  return (
    <Suspense fallback={<main className="section"><div className="state-card loading-state"><span className="spinner" />Đang tải form đánh giá...</div></main>}>
      <NewReviewContent />
    </Suspense>
  );
}

function NewReviewContent() {
  const searchParams = useSearchParams();
  const token = searchParams?.get("token") || "";
  const [invitation, setInvitation] = useState<ReviewInvitation | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ rating: 5, displayName: "", content: "" });

  useEffect(() => {
    if (!token) {
      setError("Link đánh giá không hợp lệ.");
      setLoading(false);
      return;
    }
    customerReviewsApi.invitation(token)
      .then(setInvitation)
      .catch((err) => setError(err instanceof Error ? err.message : "Link đánh giá không hợp lệ hoặc đã hết hạn."))
      .finally(() => setLoading(false));
  }, [token]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!token || !form.content.trim()) return;
    setSaving(true);
    setError(null);
    try {
      await customerReviewsApi.submitInvitation(token, {
        rating: Number(form.rating),
        displayName: form.displayName.trim() || undefined,
        content: form.content.trim()
      });
      setSubmitted(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không gửi được đánh giá.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <main className="section" style={{ minHeight: "70vh", display: "grid", placeItems: "center" }}>
      <section className="card" style={{ width: "min(720px, 100%)" }}>
        {loading ? <div className="state-card loading-state"><span className="spinner" />Đang kiểm tra link đánh giá...</div> : null}
        {!loading && submitted ? (
          <div className="state-card" style={{ background: "#e4f7ec", color: "#187344", border: "1px solid #c2ebd5" }}>
            Cảm ơn bạn đã chia sẻ trải nghiệm tại PoolHub.
          </div>
        ) : null}
        {!loading && !submitted && error ? <div className="state-card error">{error}</div> : null}
        {!loading && !submitted && invitation ? (
          <>
            <div className="section-heading" style={{ alignItems: "flex-start", textAlign: "left", marginBottom: 20 }}>
              <p className="eyebrow">Đánh giá trải nghiệm</p>
              <h1 style={{ margin: 0 }}>Cảm ơn {invitation.customerDisplayName}</h1>
              <p>
                Hóa đơn {invitation.invoiceCode} · Phiên {invitation.sessionCode}
                {invitation.tableName ? ` · ${invitation.tableName}` : ""}
              </p>
            </div>
            {!invitation.canSubmit ? (
              <div className="state-card">{invitation.reason || "Link đánh giá không còn khả dụng."}</div>
            ) : (
              <form className="form-grid" onSubmit={submit}>
                <label><span>Số sao</span><select value={form.rating} onChange={(event) => setForm({ ...form, rating: Number(event.target.value) })}><option value={5}>5</option><option value={4}>4</option><option value={3}>3</option><option value={2}>2</option><option value={1}>1</option></select></label>
                <label><span>Tên hiển thị</span><input value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} placeholder={invitation.customerDisplayName} /></label>
                <label className="full-field"><span>Nội dung đánh giá</span><textarea required rows={5} maxLength={1000} value={form.content} onChange={(event) => setForm({ ...form, content: event.target.value })} /></label>
                {error ? <div className="state-card error" style={{ gridColumn: "1 / -1" }}>{error}</div> : null}
                <div style={{ gridColumn: "1 / -1", display: "flex", justifyContent: "flex-end" }}>
                  <button className="primary-btn" disabled={saving}>{saving ? "Đang gửi..." : "Gửi đánh giá"}</button>
                </div>
              </form>
            )}
          </>
        ) : null}
      </section>
    </main>
  );
}
