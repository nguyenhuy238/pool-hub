"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/components/auth-provider";
import { MANAGEMENT_READ_ROLES, OPERATION_ROLES, ROLES } from "@/lib/auth/constants";

const nav = [
  { href: "/admin/dashboard", label: "Admin Dashboard", roles: [ROLES.ADMIN] },
  { href: "/dashboard", label: "Dashboard", roles: OPERATION_ROLES },
  { href: "/operation/floor-map", label: "Floor Map", roles: OPERATION_ROLES },
  { href: "/operation/bookings", label: "Booking", roles: OPERATION_ROLES },
  { href: "/management/customers", label: "Customers", roles: OPERATION_ROLES },
  { href: "/operation/sessions", label: "Session", roles: OPERATION_ROLES },
  { href: "/operation/orders", label: "Orders", roles: OPERATION_ROLES },
  { href: "/operation/invoices", label: "Invoices", roles: OPERATION_ROLES },
  { href: "/management/products", label: "Products", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/product-categories", label: "Categories", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/floors", label: "Floors", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/zones", label: "Zones", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/table-types", label: "Table Types", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/venue-tables", label: "Venue Tables", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/pricing-plans", label: "Pricing", roles: MANAGEMENT_READ_ROLES },
  { href: "/management/pricing-rules", label: "Pricing Rules", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/users", label: "Users", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/roles", label: "Roles", roles: MANAGEMENT_READ_ROLES },
  { href: "/admin/discounts", label: "Discounts", roles: [ROLES.ADMIN, ROLES.CASHIER] },
  { href: "/admin/inventory", label: "Inventory", roles: [ROLES.ADMIN, ROLES.MANAGER] },
  { href: "/admin/payments", label: "Payments", roles: [ROLES.ADMIN, ROLES.CASHIER] },
  { href: "/admin/reports", label: "Reports", roles: [ROLES.ADMIN, ROLES.MANAGER] },
  { href: "/admin/landing-settings", label: "Landing Page Settings", roles: [ROLES.ADMIN] },
  { href: "/admin/audit-logs", label: "Audit Logs", roles: MANAGEMENT_READ_ROLES },
  { href: "/change-password", label: "Change Password", roles: OPERATION_ROLES },
  { href: "/operation/notifications", label: "Notifications", roles: OPERATION_ROLES }
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() || "/";
  const { user, logout } = useAuth();
  const roles = user?.roles || [];
  const allowed = nav.filter((item) => item.roles.some((role) => roles.includes(role)));

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
            <div className="breadcrumb">PoolHub / {pathname.split("/").filter(Boolean).join(" / ") || "home"}</div>
            <Link href="/profile"><strong>{user?.fullName || "PoolHub"}</strong></Link>
          </div>
          <div className="topbar-actions">
            <Link className="icon-btn" href="/operation/notifications" title="Thông báo">🔔</Link>
            <span className="role-badge">{roles.join(", ") || "Guest"}</span>
            <button className="ghost-btn" onClick={logout}>Logout</button>
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
