"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";
import { ApiError } from "@/lib/api/client";
import { validateEmail } from "@/lib/validation";

export default function LoginPage() {
  const { login } = useAuth();
  const toast = useToast();
  const [form, setForm] = useState({ email: "", password: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const emailError = validateEmail(form.email);
    if (emailError) return setError(emailError);
    if (!form.password) return setError("Vui lòng nhập mật khẩu.");

    setLoading(true);
    setError("");
    try {
      await login(form.email.trim(), form.password);
      toast("Đăng nhập thành công.", "success");
    } catch (err) {
      const message = err instanceof ApiError && err.status === 401
        ? "Email hoặc mật khẩu không chính xác."
        : err instanceof ApiError && err.status === 423
          ? "Tài khoản đã bị khóa hoặc vô hiệu hóa. Vui lòng liên hệ quản trị viên."
          : err instanceof Error ? err.message : "Không thể đăng nhập.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap auth-scene">
      <form className="card auth-card form-stack" onSubmit={submit}>
        <div className="auth-heading">
          <span className="auth-mark">PH</span>
          <div><h1>Đăng nhập PoolHub</h1><p>Truy cập hệ thống quản lý vận hành quán bi-a.</p></div>
        </div>
        {error ? <div className="inline-alert error" role="alert">{error}</div> : null}
        <label><span>Email</span><input autoComplete="email" type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
        <label><span>Mật khẩu</span><input autoComplete="current-password" type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /></label>
        <button className="primary-btn" disabled={loading}>{loading ? "Đang đăng nhập..." : "Đăng nhập"}</button>
        <div className="auth-links"><Link href="/forgot-password">Quên mật khẩu?</Link><Link href="/register">Tạo tài khoản</Link></div>
        <Link className="muted-link" href="/booking">Tiếp tục đặt bàn công khai</Link>
      </form>
    </div>
  );
}
