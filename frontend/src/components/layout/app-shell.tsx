"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuth } from "@/components/auth-provider";
import { MANAGEMENT_READ_ROLES, OPERATION_ROLES, ROLES } from "@/lib/auth/constants";
import { NotificationDropdown } from "./notification-dropdown";

const nav = [
  { href: "/admin/dashboard", label: "Tổng quan quản trị", roles: [ROLES.ADMIN] },
  { href: "/dashboard", label: "Tổng quan vận hành", roles: OPERATION_ROLES },
  { href: "/operation/floor-map", label: "Sơ đồ bàn", roles: OPERATION_ROLES },
  { href: "/operation/bookings", label: "Đặt bàn", roles: OPERATION_ROLES },
  { href: "/management/customers", label: "Khách hàng", roles: OPERATION_ROLES },
  { href: "/operation/sessions", label: "Phiên chơi", roles: OPERATION_ROLES },
  { href: "/operation/orders", label: "Đơn hàng", roles: OPERATION_ROLES },
  { href: "/operation/invoices", label: "Hóa đơn", roles: OPERATION_ROLES },
  { href: "/management/products", label: "Sản phẩm", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/product-categories", label: "Danh mục sản phẩm", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/floors", label: "Tầng", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/zones", label: "Khu vực", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/table-types", label: "Loại bàn", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/venue-tables", label: "Bàn chơi", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/pricing-plans", label: "Bảng giá", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/pricing-rules", label: "Quy tắc tính giá", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/users", label: "Người dùng", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/roles", label: "Vai trò và quyền hạn", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/discounts", label: "Mã giảm giá", roles: [ROLES.ADMIN, ROLES.CASHIER] },
  { href: "/admin/inventory", label: "Tồn kho", roles: [ROLES.ADMIN, ROLES.MANAGER] },
  { href: "/admin/payments", label: "Thanh toán", roles: [ROLES.ADMIN, ROLES.CASHIER] },
  { href: "/admin/reports", label: "Báo cáo", roles: [ROLES.ADMIN, ROLES.MANAGER] },
  { href: "/admin/landing-settings", label: "Cấu hình trang chủ", roles: [ROLES.ADMIN] },
  { href: "/admin/audit-logs", label: "Nhật ký hệ thống", roles: MANAGEMENT_READ_ROLES },
  { href: "/change-password", label: "Đổi mật khẩu", roles: OPERATION_ROLES },
  { href: "/operation/notifications", label: "Thông báo", roles: OPERATION_ROLES }
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() || "/";
  const { user, logout } = useAuth();
  const roles = user?.roles || [];
  const allowed = nav.filter((item) => item.roles.some((role) => roles.includes(role)));
  const [darkMode, setDarkMode] = useState(false);

  useEffect(() => {
    const enabled = localStorage.getItem("poolhub.theme") === "dark";
    setDarkMode(enabled);
    document.documentElement.dataset.theme = enabled ? "dark" : "light";
  }, []);

  function toggleTheme() {
    const enabled = !darkMode;
    setDarkMode(enabled);
    localStorage.setItem("poolhub.theme", enabled ? "dark" : "light");
    document.documentElement.dataset.theme = enabled ? "dark" : "light";
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <Link className="brand" href="/dashboard"><span>PH</span>PoolHub</Link>
        <nav>
          {allowed.map((item) => (
            <Link key={item.href} className={pathname === item.href ? "active" : ""} href={item.href}>{item.label}</Link>
          ))}
        </nav>
      </aside>
      <div className="main-shell">
        <header className="topbar">
          <div>
            <div className="breadcrumb">PoolHub / {pathname.split("/").filter(Boolean).join(" / ") || "trang chủ"}</div>
            <Link href="/profile"><strong>{user?.fullName || "PoolHub"}</strong></Link>
          </div>
          <div className="topbar-actions" style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
            <NotificationDropdown />
            <button className="ghost-btn" onClick={toggleTheme}>{darkMode ? "Giao diện sáng" : "Giao diện tối"}</button>
            <span className="role-badge">{roles.join(", ") || "Khách"}</span>
            <button className="ghost-btn" onClick={logout}>Đăng xuất</button>
          </div>
        </header>
        <main>{children}</main>
      </div>
      <nav className="bottom-nav">
        {allowed.slice(0, 5).map((item) => (
          <Link key={item.href} className={pathname === item.href ? "active" : ""} href={item.href}>{item.label}</Link>
        ))}
      </nav>
    </div>
  );
}
