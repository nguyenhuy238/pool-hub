"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { authApi } from "@/lib/api/endpoints";
import { tokenStore } from "@/lib/api/client";
import type { AuthUser, RoleName } from "@/types";

type AuthContextValue = {
  user: AuthUser | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<AuthUser>;
  logout: () => Promise<void>;
  fetchMe: () => Promise<void>;
  hasAnyRole: (roles: RoleName[]) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function getLandingPath(roles: RoleName[]) {
  if (roles.some((role) => ["Admin", "Owner"].includes(role))) return "/admin/dashboard";
  if (roles.some((role) => ["Manager"].includes(role))) return "/dashboard";
  if (roles.some((role) => ["Staff", "Cashier"].includes(role))) return "/operation/floor-map";
  return "/booking";
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchMe = useCallback(async () => {
    if (!tokenStore.getAccess()) {
      setIsLoading(false);
      return;
    }
    try {
      setUser(await authApi.me());
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchMe().catch(() => {
      tokenStore.clear();
      setUser(null);
      setIsLoading(false);
    });
  }, [fetchMe]);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login({ email, password });
    tokenStore.set(response.accessToken, response.refreshToken);
    const currentUser = await authApi.me().catch(() => ({
      userId: response.userId,
      email: response.email,
      fullName: response.fullName,
      roles: response.roles
    }));
    setUser(currentUser);
    router.push(getLandingPath(currentUser.roles));
    return currentUser;
  }, [router]);

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.getRefresh();
    if (refreshToken) await authApi.logout(refreshToken).catch(() => undefined);
    tokenStore.clear();
    setUser(null);
    router.push("/login");
  }, [router]);

  const hasAnyRole = useCallback((roles: RoleName[]) => {
    if (!roles.length) return true;
    return Boolean(user?.roles.some((role) => roles.includes(role)));
  }, [user]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isLoading,
    isAuthenticated: Boolean(user),
    login,
    logout,
    fetchMe,
    hasAnyRole
  }), [fetchMe, hasAnyRole, isLoading, login, logout, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
