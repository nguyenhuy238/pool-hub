"use client";

import { FormEvent, useState } from "react";
import Link from "next/link";
import { useAuth } from "@/components/auth-provider";
import { useToast } from "@/components/toast";

export default function LoginPage() {
  const { login } = useAuth();
  const toast = useToast();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    try {
      await login(email, password);
      toast("Đăng nhập thành công.", "success");
    } catch (err) {
      toast(err instanceof Error ? err.message : "Đăng nhập thất bại.", "error");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-wrap">
      <form className="card auth-card form-grid" onSubmit={submit}>
        <h2>Đăng nhập PoolHub</h2>
        <label><span>Email</span><input type="email" required value={email} onChange={(event) => setEmail(event.target.value)} /></label>
        <label><span>Mật khẩu</span><input type="password" required minLength={6} value={password} onChange={(event) => setPassword(event.target.value)} /></label>
        <button className="primary-btn" disabled={loading}>{loading ? "Đang đăng nhập..." : "Đăng nhập"}</button>
        <Link href="/booking">Tiếp tục đặt bàn công khai</Link>
      </form>
    </div>
  );
}
