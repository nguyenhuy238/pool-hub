"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useToast } from "@/components/toast";
import { ApiError } from "@/lib/api/client";
import { validateEmail, validatePassword } from "@/lib/validation";
import { authService } from "@/services/auth-service";

export default function ForgotPasswordPage() {
  const router = useRouter();
  const toast = useToast();
  const [email, setEmail] = useState("");
  const [form, setForm] = useState({ otp: "", newPassword: "", confirmPassword: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [otpSent, setOtpSent] = useState(false);

  async function sendOtp(event?: FormEvent) {
    event?.preventDefault();
    const validationError = validateEmail(email);
    if (validationError) return setError(validationError);

    setLoading(true);
    setError("");
    try {
      await authService.forgotPassword({ email: email.trim() });
      setOtpSent(true);
      toast("Nếu email tồn tại, mã OTP đã được gửi.", "success");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 503
        ? "Không thể gửi email OTP lúc này. Vui lòng thử lại sau."
        : err instanceof ApiError && err.status === 429
          ? "Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ một phút rồi thử lại."
          : err instanceof Error ? err.message : "Không thể gửi mã OTP.");
    } finally {
      setLoading(false);
    }
  }

  async function resetPassword(event: FormEvent) {
    event.preventDefault();
    const validationError = validateEmail(email)
      || (!/^\d{6}$/.test(form.otp) ? "Mã OTP phải gồm đúng 6 chữ số." : "")
      || validatePassword(form.newPassword)
      || (form.newPassword !== form.confirmPassword ? "Xác nhận mật khẩu không khớp." : "");
    if (validationError) return setError(validationError);

    setLoading(true);
    setError("");
    try {
      await authService.resetPassword({ email: email.trim(), ...form });
      toast("Đặt lại mật khẩu thành công. Vui lòng đăng nhập.", "success");
      router.replace("/?login=customer");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401
        ? "Mã OTP không đúng hoặc đã hết hạn."
        : err instanceof Error ? err.message : "Không thể đặt lại mật khẩu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap auth-scene">
      <form className="card auth-card form-stack" onSubmit={otpSent ? resetPassword : sendOtp}>
        <div className="auth-heading">
          <span className="auth-mark">OTP</span>
          <div>
            <h1>Quên mật khẩu</h1>
            <p>{otpSent ? "Nhập mã OTP trong email và tạo mật khẩu mới." : "Nhập email tài khoản khách hàng để nhận mã OTP."}</p>
          </div>
        </div>
        {otpSent ? <div className="inline-alert success">Nếu email tồn tại, mã OTP 6 chữ số đã được gửi đến {email.trim()}.</div> : null}
        {error ? <div className="inline-alert error" role="alert">{error}</div> : null}
        <label>
          <span>Email</span>
          <input type="email" autoComplete="email" value={email} disabled={otpSent} onChange={(event) => setEmail(event.target.value)} />
        </label>
        {otpSent ? (
          <>
            <label>
              <span>Mã OTP</span>
              <input
                inputMode="numeric"
                autoComplete="one-time-code"
                maxLength={6}
                value={form.otp}
                onChange={(event) => setForm({ ...form, otp: event.target.value.replace(/\D/g, "").slice(0, 6) })}
              />
            </label>
            <label>
              <span>Mật khẩu mới</span>
              <input type="password" autoComplete="new-password" value={form.newPassword} onChange={(event) => setForm({ ...form, newPassword: event.target.value })} />
            </label>
            <p className="field-help">Ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt.</p>
            <label>
              <span>Xác nhận mật khẩu</span>
              <input type="password" autoComplete="new-password" value={form.confirmPassword} onChange={(event) => setForm({ ...form, confirmPassword: event.target.value })} />
            </label>
            <button className="primary-btn" disabled={loading}>{loading ? "Đang cập nhật..." : "Đặt lại mật khẩu"}</button>
            <div className="auth-links">
              <button className="muted-link link-button" type="button" disabled={loading} onClick={() => void sendOtp()}>Gửi lại OTP</button>
              <button className="muted-link link-button" type="button" disabled={loading} onClick={() => { setOtpSent(false); setError(""); }}>Đổi email</button>
            </div>
          </>
        ) : <button className="primary-btn" disabled={loading}>{loading ? "Đang gửi..." : "Gửi mã OTP"}</button>}
        <Link className="muted-link" href="/?login=customer">Quay lại đăng nhập</Link>
      </form>
    </div>
  );
}
