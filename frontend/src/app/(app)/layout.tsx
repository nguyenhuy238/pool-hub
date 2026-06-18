"use client";

import { usePathname } from "next/navigation";
import { AppShell } from "@/components/layout/app-shell";
import { ProtectedRoute } from "@/components/guards";
import type { RoleName } from "@/types";

function rolesForPath(pathname: string): RoleName[] {
  if (pathname.startsWith("/admin/audit-logs")) return ["Admin", "Owner", "Manager"];
  if (pathname.startsWith("/admin/landing-settings")) return ["Admin"];
  if (pathname.startsWith("/admin")) return ["Admin", "Owner"];
  if (pathname.startsWith("/management")) return ["Admin", "Owner", "Manager"];
  if (pathname.startsWith("/operation")) return ["Admin", "Owner", "Manager", "Staff", "Cashier"];
  if (pathname.startsWith("/dashboard")) return ["Admin", "Owner", "Manager", "Staff", "Cashier"];
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
