"use client";

import { FormEvent, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";
import { validatePassword } from "@/lib/validation";
import { authService } from "@/services/auth-service";

export function ChangePasswordForm() {
  const { logout } = useAuth();
  const toast = useToast();
  const [form, setForm] = useState({ currentPassword: "", newPassword: "", confirmPassword: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationError = !form.currentPassword
      ? "Vui lòng nhập mật khẩu hiện tại."
      : validatePassword(form.newPassword)
        || (form.newPassword !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "");

    if (validationError) {
      setError(validationError);
      return;
    }

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
    <form className="card form-stack narrow-form" onSubmit={submit}>
      {error ? <div className="inline-alert error" role="alert">{error}</div> : null}
      <label>
        <span>Mật khẩu hiện tại</span>
        <input
          type="password"
          autoComplete="current-password"
          value={form.currentPassword}
          onChange={(event) => setForm({ ...form, currentPassword: event.target.value })}
          disabled={loading}
          required
        />
      </label>
      <label>
        <span>Mật khẩu mới</span>
        <input
          type="password"
          autoComplete="new-password"
          value={form.newPassword}
          onChange={(event) => setForm({ ...form, newPassword: event.target.value })}
          disabled={loading}
          minLength={8}
          aria-describedby="new-password-help"
          required
        />
        <small className="field-help" id="new-password-help">
          Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.
        </small>
      </label>
      <label>
        <span>Xác nhận mật khẩu mới</span>
        <input
          type="password"
          autoComplete="new-password"
          value={form.confirmPassword}
          onChange={(event) => setForm({ ...form, confirmPassword: event.target.value })}
          disabled={loading}
          minLength={8}
          required
        />
      </label>
      <button className="primary-btn" type="submit" disabled={loading}>
        {loading ? "Đang cập nhật..." : "Đổi mật khẩu"}
      </button>
    </form>
  );
}
