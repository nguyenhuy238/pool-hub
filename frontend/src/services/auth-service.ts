import { apiFetch, refreshAccessToken } from "@/lib/api/client";
import type {
  AuthResponse,
  AuthUser,
  ChangePasswordRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest
} from "@/types";

export const authService = {
  login: (payload: LoginRequest, portal: "customer" | "admin" = "customer") =>
    apiFetch<AuthResponse>(portal === "admin" ? "/api/auth/admin/login" : "/api/auth/customer/login", {
      method: "POST",
      body: JSON.stringify(payload),
      skipAuth: true,
      timeoutMs: 15_000
    }),
  register: (payload: RegisterRequest) =>
    apiFetch<AuthResponse>("/api/auth/register", { method: "POST", body: JSON.stringify(payload), skipAuth: true }),
  getMe: () => apiFetch<AuthUser>("/api/auth/me"),
  logout: () =>
    apiFetch("/api/auth/logout", { method: "POST", retry: false }),
  refreshToken: refreshAccessToken,
  forgotPassword: (payload: ForgotPasswordRequest) =>
    apiFetch("/api/auth/forgot-password", { method: "POST", body: JSON.stringify(payload), skipAuth: true }),
  resetPassword: (payload: ResetPasswordRequest) =>
    apiFetch("/api/auth/reset-password", { method: "POST", body: JSON.stringify(payload), skipAuth: true }),
  changePassword: (payload: ChangePasswordRequest) =>
    apiFetch("/api/auth/change-password", { method: "PUT", body: JSON.stringify(payload) })
};
