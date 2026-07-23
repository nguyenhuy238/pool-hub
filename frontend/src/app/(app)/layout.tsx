"use client";

import { useMemo } from "react";
import { usePathname } from "next/navigation";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/components/guards";
import type { RoleName } from "@/types";
import { DASHBOARD_ROLES, MANAGEMENT_READ_ROLES, OPERATION_ROLES, PERMISSIONS, ROLES } from "@/lib/auth/constants";

type RouteAccess = { roles?: RoleName[]; permissions?: string[] };

function accessForPath(pathname: string): RouteAccess {
  if (pathname.startsWith("/admin/audit-logs")) return { roles: ["Admin", "Owner", "Manager"], permissions: [PERMISSIONS.AUDIT_VIEW] };
  if (pathname.startsWith("/admin/users") || pathname.startsWith("/admin/roles")) return { roles: [ROLES.ADMIN] };
  if (pathname.startsWith("/admin/landing-settings") || pathname.startsWith("/admin/customer-reviews")) return { roles: [ROLES.ADMIN, ROLES.MANAGER] };
  if (pathname.startsWith("/admin/discounts")) return { roles: OPERATION_ROLES, permissions: [PERMISSIONS.DISCOUNTS_MANAGE] };
  if (pathname.startsWith("/admin/payments")) return { roles: OPERATION_ROLES, permissions: [PERMISSIONS.PAYMENTS_MANAGE] };
  if (pathname.startsWith("/admin/inventory")) return { roles: MANAGEMENT_READ_ROLES, permissions: [PERMISSIONS.INVENTORY_MANAGE] };
  if (pathname.startsWith("/admin/reports") || pathname.startsWith("/admin/analytics") || pathname.startsWith("/admin/dashboard")) return {
    roles: MANAGEMENT_READ_ROLES,
    permissions: [PERMISSIONS.REPORTS_VIEW]
  };
  if (pathname.startsWith("/admin")) return { roles: [ROLES.ADMIN] };
  if (pathname.startsWith("/management")) return { roles: MANAGEMENT_READ_ROLES };
  if (pathname.startsWith("/operation")) return { roles: OPERATION_ROLES };
  if (pathname.startsWith("/profile") || pathname.startsWith("/change-password")) return {};
  if (pathname.startsWith("/notifications")) return {};
  if (pathname.startsWith("/dashboard")) return { roles: DASHBOARD_ROLES };
  return {};
}

export default function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() || "/";
  const access = useMemo(() => accessForPath(pathname), [pathname]);
  return (
    <ProtectedRoute roles={access.roles} permissions={access.permissions}>
      <AppShell>{children}</AppShell>
    </ProtectedRoute>
  );
}
