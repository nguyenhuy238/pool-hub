"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { authService } from "@/services/auth-service";
import { validateEmail } from "@/lib/validation";
import { ApiError } from "@/lib/api/client";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [sent, setSent] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const validationError = validateEmail(email);
    if (validationError) return setError(validationError);
    setLoading(true);
    setError("");
    try {
      await authService.forgotPassword({ email: email.trim() });
      setSent(true);
    } catch (err) {
      setError(err instanceof ApiError && err.status === 503
        ? "Dịch vụ gửi email tạm thời chưa sẵn sàng. Vui lòng thử lại sau."
        : err instanceof ApiError && err.status === 429
          ? "Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ một phút rồi thử lại."
          : err instanceof Error ? err.message : "Không thể gửi yêu cầu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap auth-scene">
      <form className="card auth-card form-stack" onSubmit={submit}>
        <div className="auth-heading"><span className="auth-mark">?</span><div><h1>Quên mật khẩu</h1><p>Nhập email đã đăng ký để nhận hướng dẫn đặt lại mật khẩu.</p></div></div>
        {sent ? <div className="inline-alert success">Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi.</div> : null}
        {error ? <div className="inline-alert error">{error}</div> : null}
        <label><span>Email</span><input type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} /></label>
        <button className="primary-btn" disabled={loading}>{loading ? "Đang gửi..." : "Gửi hướng dẫn"}</button>
        <Link className="muted-link" href="/login">Quay lại đăng nhập</Link>
      </form>
    </div>
  );
}
