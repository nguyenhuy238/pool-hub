"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { tokenStore } from "@/lib/api/client";
import { landingPathFor } from "@/lib/auth/constants";
import { authService } from "@/services/auth-service";
import type { AuthUser, RegisterRequest, RoleName } from "@/types";

type AuthContextValue = {
  user: AuthUser | null;
  roles: RoleName[];
  accessToken: string | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<AuthUser>;
  register: (payload: RegisterRequest) => Promise<AuthUser>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<boolean>;
  fetchMe: () => Promise<AuthUser | null>;
  hasRole: (role: RoleName) => boolean;
  hasAnyRole: (roles: RoleName[]) => boolean;
  clearAuth: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export const getLandingPath = landingPathFor;

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const clearAuth = useCallback(() => {
    tokenStore.clear();
    setAccessToken(null);
    setUser(null);
  }, []);

  const fetchMe = useCallback(async () => {
    const token = tokenStore.getAccess();
    setAccessToken(token);
    if (!token) {
      setUser(null);
      setIsLoading(false);
      return null;
    }
    try {
      const currentUser = await authService.getMe();
      setUser(currentUser);
      return currentUser;
    } catch {
      clearAuth();
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [clearAuth]);

  useEffect(() => {
    void fetchMe();
  }, [fetchMe]);

  useEffect(() => {
    const handleInvalidAuth = () => {
      clearAuth();
      router.replace("/login");
    };
    window.addEventListener("poolhub:auth-invalid", handleInvalidAuth);
    return () => window.removeEventListener("poolhub:auth-invalid", handleInvalidAuth);
  }, [clearAuth, router]);

  const establishSession = useCallback(async (response: Awaited<ReturnType<typeof authService.login>>) => {
    tokenStore.set(response.accessToken, response.refreshToken);
    setAccessToken(response.accessToken);
    const fallback = response.user ?? {
      userId: response.userId,
      publicId: response.publicId,
      email: response.email,
      fullName: response.fullName,
      roles: response.roles
    };
    const currentUser = await authService.getMe().catch(() => fallback);
    setUser(currentUser);
    return currentUser;
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const currentUser = await establishSession(await authService.login({ email, password }));
    router.replace(landingPathFor(currentUser.roles));
    return currentUser;
  }, [establishSession, router]);

  const register = useCallback(async (payload: RegisterRequest) => {
    const currentUser = await establishSession(await authService.register(payload));
    router.replace(landingPathFor(currentUser.roles));
    return currentUser;
  }, [establishSession, router]);

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.getRefresh();
    if (refreshToken) await authService.logout(refreshToken).catch(() => undefined);
    clearAuth();
    router.replace("/login");
  }, [clearAuth, router]);

  const refreshToken = useCallback(async () => {
    const refreshed = await authService.refreshToken();
    setAccessToken(tokenStore.getAccess());
    if (!refreshed) clearAuth();
    return refreshed;
  }, [clearAuth]);

  const hasRole = useCallback((role: RoleName) => Boolean(user?.roles.includes(role)), [user]);
  const hasAnyRole = useCallback((roles: RoleName[]) => {
    if (!roles.length) return true;
    return Boolean(user?.roles.some((role) => roles.includes(role)));
  }, [user]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    roles: user?.roles ?? [],
    accessToken,
    isLoading,
    isAuthenticated: Boolean(user),
    login,
    register,
    logout,
    refreshToken,
    fetchMe,
    hasRole,
    hasAnyRole,
    clearAuth
  }), [accessToken, clearAuth, fetchMe, hasAnyRole, hasRole, isLoading, login, logout, refreshToken, register, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
