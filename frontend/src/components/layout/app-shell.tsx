"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/components/auth-provider";

const nav = [
  { href: "/dashboard", label: "Dashboard", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/operation/floor-map", label: "Floor Map", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/operation/bookings", label: "Booking", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/operation/sessions", label: "Session", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/operation/orders", label: "Orders", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/operation/invoices", label: "Invoices", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] },
  { href: "/management/products", label: "Products", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/product-categories", label: "Categories", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/floors", label: "Floors", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/zones", label: "Zones", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/table-types", label: "Table Types", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/venue-tables", label: "Venue Tables", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/pricing-plans", label: "Pricing", roles: ["Admin", "Owner", "Manager"] },
  { href: "/management/pricing-rules", label: "Pricing Rules", roles: ["Admin", "Owner", "Manager"] },
  { href: "/admin/users", label: "Users", roles: ["Admin", "Owner"] },
  { href: "/admin/roles", label: "Roles", roles: ["Admin", "Owner"] },
  { href: "/admin/audit-logs", label: "Audit Logs", roles: ["Admin", "Owner", "Manager"] },
  { href: "/operation/notifications", label: "Notifications", roles: ["Admin", "Owner", "Manager", "Staff", "Cashier"] }
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
            <strong>{user?.fullName || "PoolHub"}</strong>
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
