"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useToast } from "@/components/toast";
import { authService } from "@/services/auth-service";
import { validateEmail, validatePassword } from "@/lib/validation";

export function ResetPasswordForm() {
  const params = useSearchParams();
  const router = useRouter();
  const toast = useToast();
  const [email, setEmail] = useState(params?.get("email") ?? "");
  const token = params?.get("token") ?? "";
  const [form, setForm] = useState({ newPassword: "", confirmPassword: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(token ? "" : "Liên kết đặt lại mật khẩu thiếu token.");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const validationError = validateEmail(email) || (!token ? "Token đặt lại mật khẩu không hợp lệ." : "")
      || validatePassword(form.newPassword)
      || (form.newPassword !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "");
    if (validationError) return setError(validationError);
    setLoading(true);
    setError("");
    try {
      await authService.resetPassword({ email: email.trim(), token, ...form });
      toast("Đặt lại mật khẩu thành công. Vui lòng đăng nhập.", "success");
      router.replace("/login");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Token không hợp lệ hoặc đã hết hạn.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap auth-scene">
      <form className="card auth-card form-stack" onSubmit={submit}>
        <div className="auth-heading"><span className="auth-mark">↻</span><div><h1>Đặt lại mật khẩu</h1><p>Tạo mật khẩu mới cho tài khoản PoolHub.</p></div></div>
        {error ? <div className="inline-alert error">{error}</div> : null}
        <label><span>Email</span><input type="email" value={email} onChange={(e) => setEmail(e.target.value)} /></label>
        <label><span>Mật khẩu mới</span><input type="password" autoComplete="new-password" value={form.newPassword} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} /></label>
        <label><span>Xác nhận mật khẩu</span><input type="password" autoComplete="new-password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} /></label>
        <button className="primary-btn" disabled={loading || !token}>{loading ? "Đang cập nhật..." : "Đặt lại mật khẩu"}</button>
        <Link className="muted-link" href="/forgot-password">Yêu cầu liên kết mới</Link>
      </form>
    </div>
  );
}
