"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";
import { PageHeader } from "@/components/ui";
import { validatePassword } from "@/lib/validation";
import { authService } from "@/services/auth-service";

export default function ChangePasswordPage() {
  const { logout } = useAuth();
  const toast = useToast();
  const [form, setForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const validationError = !form.currentPassword ? "Vui lòng nhập mật khẩu hiện tại."
      : validatePassword(form.newPassword)
      || (form.newPassword !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "");
    if (validationError) return setError(validationError);
    setLoading(true);
    setError("");
    try {
      await authService.changePassword(form);
      toast("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.", "success");
      await logout();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể đổi mật khẩu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <>
      <PageHeader title="Đổi mật khẩu" description="Sau khi đổi mật khẩu, các refresh token hiện tại sẽ bị thu hồi." />
      <form className="card form-stack narrow-form" onSubmit={submit}>
        {error ? <div className="inline-alert error">{error}</div> : null}
        <label><span>Mật khẩu hiện tại</span><input type="password" autoComplete="current-password" value={form.currentPassword} onChange={(e) => setForm({ ...form, currentPassword: e.target.value })} /></label>
        <label><span>Mật khẩu mới</span><input type="password" autoComplete="new-password" value={form.newPassword} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} /></label>
        <label><span>Xác nhận mật khẩu mới</span><input type="password" autoComplete="new-password" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} /></label>
        <button className="primary-btn" disabled={loading}>{loading ? "Đang cập nhật..." : "Đổi mật khẩu"}</button>
      </form>
    </>
  );
}
