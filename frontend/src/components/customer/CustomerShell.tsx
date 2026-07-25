"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/components/auth-provider";

const navItems = [
  { href: "/customer", label: "Tổng quan", exact: true },
  { href: "/customer/bookings", label: "Lịch sử booking" },
  { href: "/customer/sessions", label: "Lịch sử phiên chơi" },
  { href: "/customer/invoices", label: "Lịch sử hóa đơn" },
  { href: "/customer/vouchers", label: "Voucher của tôi" },
  { href: "/customer/points", label: "Lịch sử điểm" },
  { href: "/customer/change-password", label: "Đổi mật khẩu" }
];

export function CustomerShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() || "/customer";
  const { user, logout } = useAuth();

  return (
    <div className="customer-portal">
      <aside className="customer-sidebar">
        <Link className="customer-brand" href="/customer">
          <span>PH</span>
          <div><strong>PoolHub</strong><small>Khu vực khách hàng</small></div>
        </Link>
        <nav className="customer-nav" aria-label="Điều hướng tài khoản">
          {navItems.map((item) => {
            const active = item.exact ? pathname === item.href : pathname.startsWith(item.href);
            return <Link className={active ? "active" : ""} href={item.href} key={item.href}>{item.label}</Link>;
          })}
        </nav>
        <div className="customer-sidebar-footer">
          <Link href="/">Xem trang giới thiệu</Link>
          <button type="button" onClick={logout}>Đăng xuất</button>
        </div>
      </aside>
      <div className="customer-main">
        <header className="customer-topbar">
          <div>
            <span className="customer-kicker">Tài khoản của bạn</span>
            <h1>{user?.fullName || "Khách hàng"}</h1>
          </div>
          <div className="customer-user-chip">
            <span>{(user?.fullName || "KH").slice(0, 2).toUpperCase()}</span>
            <div><strong>{user?.fullName || "Khách hàng"}</strong><small>{user?.email}</small></div>
          </div>
        </header>
        <main className="customer-content">{children}</main>
      </div>
    </div>
  );
}
