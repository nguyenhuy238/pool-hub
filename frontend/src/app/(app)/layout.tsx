"use client";

import { usePathname } from "next/navigation";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/components/guards";
import type { RoleName } from "@/types";
import { MANAGEMENT_READ_ROLES, OPERATION_ROLES, ROLES } from "@/lib/auth/constants";

function rolesForPath(pathname: string): RoleName[] {
  if (pathname.startsWith("/admin/audit-logs")) return ["Admin", "Owner", "Manager"];
  if (pathname.startsWith("/admin/users") || pathname.startsWith("/admin/roles")) return [ROLES.ADMIN];
  if (pathname.startsWith("/admin/landing-settings")) return [ROLES.ADMIN, ROLES.MANAGER];
  if (pathname.startsWith("/admin/discounts") || pathname.startsWith("/admin/payments")) return [ROLES.ADMIN, ROLES.CASHIER];
  if (pathname.startsWith("/admin/inventory") || pathname.startsWith("/admin/reports")) return MANAGEMENT_READ_ROLES;
  if (pathname.startsWith("/admin")) return [ROLES.ADMIN];
  if (pathname.startsWith("/management")) return MANAGEMENT_READ_ROLES;
  if (pathname.startsWith("/operation")) return OPERATION_ROLES;
  if (pathname.startsWith("/profile") || pathname.startsWith("/change-password")) return [];
  if (pathname.startsWith("/notifications")) return [];
  if (pathname.startsWith("/dashboard")) return OPERATION_ROLES;
  return [];
}

export default function AuthenticatedLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname() || "/";
  return (
    <ProtectedRoute roles={rolesForPath(pathname)}>
      <AppShell>{children}</AppShell>
    </ProtectedRoute>
  );
}
