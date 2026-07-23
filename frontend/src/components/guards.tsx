"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/components/auth-provider";
import { isInternalPath } from "@/lib/auth/routes";
import type { RoleName } from "@/types";

function hasAccess(userRoles: RoleName[], userPermissions: string[], roles: RoleName[], permissions: string[]) {
  const roleAllowed = !roles.length || userRoles.some((role) => roles.includes(role));
  const permissionAllowed = !permissions.length || userPermissions.some((permission) => permissions.includes(permission));
  return roleAllowed && permissionAllowed;
}

export function ProtectedRoute({ children, roles = [], permissions = [], loginPath: explicitLoginPath }: {
  children: React.ReactNode;
  roles?: RoleName[];
  permissions?: string[];
  loginPath?: string;
}) {
  const { isAuthenticated, isLoading, roles: userRoles, user } = useAuth();
  const router = useRouter();
  const pathname = usePathname() || "";
  const loginPath = explicitLoginPath;
  const allowed = hasAccess(userRoles, user?.permissions ?? [], roles, permissions);

  useEffect(() => {
    if (isLoading) return;
    if (!isAuthenticated) {
      router.replace(loginPath || (isInternalPath(pathname) ? "/admin/login" : "/"));
    }
    else if (!allowed) router.replace("/403");
  }, [allowed, isAuthenticated, isLoading, loginPath, pathname, router]);

  if (isLoading) return <div className="state-card">Đang kiểm tra phiên đăng nhập...</div>;
  if (!isAuthenticated || !allowed) return null;
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
