"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/components/auth-provider";
import type { RoleName } from "@/types";

export function ProtectedRoute({ children, roles = [] }: { children: React.ReactNode; roles?: RoleName[] }) {
  const { isAuthenticated, isLoading, hasAnyRole } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated) router.replace("/login");
    else if (!hasAnyRole(roles)) router.replace("/403");
  }, [hasAnyRole, isAuthenticated, isLoading, router, roles]);

  if (isLoading) return <div className="state-card">Đang kiểm tra phiên đăng nhập...</div>;
  if (!isAuthenticated || !hasAnyRole(roles)) return null;
  return <>{children}</>;
}

export function RoleGuard({ children, roles, fallback = null }: {
  children: React.ReactNode;
  roles: RoleName[];
  fallback?: React.ReactNode;
}) {
  const { hasAnyRole } = useAuth();
  return hasAnyRole(roles) ? <>{children}</> : <>{fallback}</>;
}
