"use client";

import Link from "next/link";
import { useAuth } from "@/components/auth-provider";
import { Badge, PageHeader } from "@/components/ui";

export default function ProfilePage() {
  const { user } = useAuth();
  return <>
    <PageHeader title="Hồ sơ" description="Thông tin tài khoản đang đăng nhập." />
    <section className="card profile-card">
      <div className="profile-avatar">{user?.fullName?.slice(0, 2).toUpperCase() || "PH"}</div>
      <div><span>Họ tên</span><strong>{user?.fullName}</strong></div>
      <div><span>Email</span><strong>{user?.email}</strong></div>
      <div><span>Roles</span><div className="badge-list">{user?.roles.map((role) => <Badge tone="blue" key={role}>{role}</Badge>)}</div></div>
      <Link className="primary-btn" href="/change-password">Đổi mật khẩu</Link>
    </section>
  </>;
}
