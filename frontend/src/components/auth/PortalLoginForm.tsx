"use client";

import { FormEvent, useEffect, useId, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";
import { ApiError } from "@/lib/api/client";
import { INTERNAL_LOGIN_ROLES, landingPathFor } from "@/lib/auth/constants";
import { validateEmail } from "@/lib/validation";
import { authService } from "@/services/auth-service";

type Portal = "customer" | "admin";

export function PortalLoginForm({
  portal,
  presentation = "page",
  onClose
}: {
  portal: Portal;
  presentation?: "page" | "modal";
  onClose?: () => void;
}) {
  const router = useRouter();
  const { clearAuth, login } = useAuth();
  const toast = useToast();
  const headingId = useId();
  const [form, setForm] = useState({ email: "", password: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const isAdmin = portal === "admin";
  const isModal = presentation === "modal";

  useEffect(() => {
    if (!isModal) return;

    const previousOverflow = document.body.style.overflow;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose?.();
    };

    document.body.style.overflow = "hidden";
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [isModal, onClose]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const emailError = validateEmail(form.email);
    if (emailError) return setError(emailError);
    if (!form.password) return setError("Vui lòng nhập mật khẩu.");

    setLoading(true);
    setError("");
    try {
      const currentUser = await login(form.email.trim(), form.password, portal);
      const isAllowed = isAdmin
        ? currentUser.roles.some((role) => INTERNAL_LOGIN_ROLES.includes(role))
        : currentUser.roles.includes("Customer");
      if (!isAllowed) {
        // The backend normally rejects a role mismatch. Keep this defensive
        // cleanup so a misconfigured/mixed-role response cannot leave the
        // wrong portal authenticated after showing the error.
        clearAuth();
        await authService.logout().catch(() => undefined);
        setError(isAdmin
          ? "Tài khoản này không có quyền truy cập khu vực vận hành."
          : "Tài khoản này không phải tài khoản khách hàng.");
        return;
      }

      toast("Đăng nhập thành công.", "success");
      const destinationRoles = isAdmin
        ? currentUser.roles.filter((role) => INTERNAL_LOGIN_ROLES.includes(role))
        : currentUser.roles;
      router.replace(isAdmin ? landingPathFor(destinationRoles) : "/customer");
    } catch (err) {
      const message = err instanceof ApiError && err.status === 401
        ? "Email hoặc mật khẩu không chính xác."
        : err instanceof ApiError && err.status === 403
          ? (isAdmin
            ? "Tài khoản không thuộc nhóm Admin, Quản lý, Nhân viên hoặc Thu ngân."
            : "Tài khoản không thuộc nhóm Customer.")
          : err instanceof ApiError && err.status === 423
            ? "Tài khoản đã bị khóa hoặc vô hiệu hóa."
            : err instanceof Error ? err.message : "Không thể đăng nhập.";
      setError(message);
      toast(message, "error");
    } finally {
      setLoading(false);
    }
  }

  const loginForm = (
    <form className="card auth-card form-stack" onSubmit={submit}>
        {isModal ? (
          <button className="login-modal-close" type="button" aria-label="Đóng đăng nhập" onClick={onClose}>
            ×
          </button>
        ) : null}
        <div className="auth-heading">
          <span className="auth-mark">PH</span>
          <div>
            <p className="auth-eyebrow">{isAdmin ? "Khu vực nội bộ" : "Chào mừng trở lại"}</p>
            <h1 id={headingId}>{isAdmin ? "Đăng nhập quản trị" : "Đăng nhập khách hàng"}</h1>
            <p>{isAdmin
              ? "Dành cho Admin, Quản lý, Nhân viên và Thu ngân."
              : "Theo dõi đặt bàn, phiên chơi, hóa đơn, voucher và điểm tích lũy."}</p>
          </div>
        </div>
        {error ? <div className="inline-alert error" role="alert">{error}</div> : null}
        <label>
          <span>Email</span>
          <input
            autoFocus
            autoComplete="email"
            type="email"
            value={form.email}
            onChange={(event) => setForm({ ...form, email: event.target.value })}
          />
        </label>
        <label>
          <span>Mật khẩu</span>
          <input
            autoComplete="current-password"
            type="password"
            value={form.password}
            onChange={(event) => setForm({ ...form, password: event.target.value })}
          />
        </label>
        <button className="primary-btn" disabled={loading}>
          {loading ? "Đang đăng nhập..." : "Đăng nhập"}
        </button>
        <div className="auth-links">
          <Link href="/forgot-password" onClick={isModal ? onClose : undefined}>Quên mật khẩu?</Link>
          {!isAdmin ? <Link href="/register" onClick={isModal ? onClose : undefined}>Tạo tài khoản</Link> : null}
        </div>
        {!isAdmin && !isModal ? <Link className="muted-link" href="/">Xem trang giới thiệu và đặt bàn công khai</Link> : null}
    </form>
  );

  if (isModal) {
    return (
      <div
        className="login-modal-backdrop"
        onMouseDown={(event) => {
          if (event.currentTarget === event.target) onClose?.();
        }}
      >
        <div className="login-modal-shell" role="dialog" aria-modal="true" aria-labelledby={headingId}>
          {loginForm}
        </div>
      </div>
    );
  }

  return (
    <div className={`auth-wrap auth-scene portal-login ${isAdmin ? "admin-login-scene" : "customer-login-scene"}`}>
      {loginForm}
    </div>
  );
}
