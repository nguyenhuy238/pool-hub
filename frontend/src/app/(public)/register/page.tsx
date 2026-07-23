"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";
import { validateEmail, validatePassword } from "@/lib/validation";

export default function RegisterPage() {
  const { register } = useAuth();
  const toast = useToast();
  const [form, setForm] = useState({ fullName: "", email: "", phoneNumber: "", password: "", confirmPassword: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const validationError = !form.fullName.trim() ? "Vui lòng nhập họ tên."
      : validateEmail(form.email) || validatePassword(form.password)
      || (!form.phoneNumber.trim() ? "Vui lòng nhập số điện thoại để liên kết hồ sơ khách hàng." : "")
      || (form.password !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "");
    if (validationError) return setError(validationError);

    setLoading(true);
    setError("");
    try {
      await register({ ...form, fullName: form.fullName.trim(), email: form.email.trim() });
      toast("Tạo tài khoản thành công.", "success");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tạo tài khoản.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap auth-scene">
      <form className="card auth-card form-stack" onSubmit={submit}>
        <div className="auth-heading"><span className="auth-mark">PH</span><div><h1>Tạo tài khoản</h1><p>Tài khoản đăng ký công khai được gán quyền Customer.</p></div></div>
        {error ? <div className="inline-alert error" role="alert">{error}</div> : null}
        <label><span>Họ tên</span><input autoComplete="name" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
        <label><span>Email</span><input autoComplete="email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
        <label><span>Số điện thoại</span><input required autoComplete="tel" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
        <label><span>Mật khẩu</span><input autoComplete="new-password" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label>
        <p className="field-help">Ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.</p>
        <label><span>Xác nhận mật khẩu</span><input autoComplete="new-password" type="password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} /></label>
        <button className="primary-btn" disabled={loading}>{loading ? "Đang tạo..." : "Đăng ký"}</button>
        <div className="auth-links"><span>Đã có tài khoản?</span><Link href="/login">Đăng nhập</Link></div>
      </form>
    </div>
  );
}
