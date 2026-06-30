"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { landingPathFor } from "@/lib/auth/constants";
import { authService } from "@/services/auth-service";
import type { AuthUser, RegisterRequest, RoleName } from "@/types";

type AuthContextValue = {
  user: AuthUser | null;
  roles: RoleName[];
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
  const [isLoading, setIsLoading] = useState(true);

  const clearAuth = useCallback(() => {
    setUser(null);
  }, []);

  const fetchMe = useCallback(async () => {
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
    await authService.logout().catch(() => undefined);
    clearAuth();
    router.replace("/login");
  }, [clearAuth, router]);

  const refreshToken = useCallback(async () => {
    const refreshed = await authService.refreshToken();
    if (refreshed) await fetchMe();
    else clearAuth();
    return refreshed;
  }, [clearAuth, fetchMe]);

  const hasRole = useCallback((role: RoleName) => Boolean(user?.roles.includes(role)), [user]);
  const hasAnyRole = useCallback((roles: RoleName[]) => {
    if (!roles.length) return true;
    return Boolean(user?.roles.some((role) => roles.includes(role)));
  }, [user]);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    roles: user?.roles ?? [],
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
  }), [clearAuth, fetchMe, hasAnyRole, hasRole, isLoading, login, logout, refreshToken, register, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
