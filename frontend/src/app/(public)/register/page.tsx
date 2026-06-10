"use client";

import { FormEvent, useState } from "react";
import { authApi } from "@/lib/api/endpoints";
import { useToast } from "@/components/toast";

export default function RegisterPage() {
  const toast = useToast();
  const [form, setForm] = useState({ fullName: "", email: "", role: "Customer", password: "" });
  async function submit(event: FormEvent) {
    event.preventDefault();
    try {
      await authApi.register(form);
      toast("Tạo tài khoản thành công.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Backend hiện chỉ cho Admin đăng ký user.", "error");
    }
  }
  return (
    <div className="auth-wrap">
      <form className="card auth-card form-grid" onSubmit={submit}>
        <h2>Đăng ký</h2>
        <label><span>Họ tên</span><input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
        <label><span>Email</span><input type="email" required value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
        <input type="hidden" value={form.role} readOnly />
        <label><span>Mật khẩu</span><input type="password" required minLength={6} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} /></label>
        <button className="primary-btn">Đăng ký</button>
      </form>
    </div>
  );
}
