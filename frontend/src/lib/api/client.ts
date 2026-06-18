"use client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";

export class ApiError extends Error {
  status: number;
  errors: string[];

  constructor(message: string, status: number, errors: string[] = []) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
}

type RequestOptions = RequestInit & { skipAuth?: boolean; retry?: boolean };

const tokenKey = "poolhub.accessToken";
const refreshKey = "poolhub.refreshToken";

export const tokenStore = {
  getAccess: () => (typeof window === "undefined" ? null : localStorage.getItem(tokenKey)),
  getRefresh: () => (typeof window === "undefined" ? null : localStorage.getItem(refreshKey)),
  set: (accessToken: string, refreshToken: string) => {
    localStorage.setItem(tokenKey, accessToken);
    localStorage.setItem(refreshKey, refreshToken);
  },
  clear: () => {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(refreshKey);
  }
};

function normalize<T>(payload: unknown): { data: T; message: string } {
  if (payload && typeof payload === "object" && "success" in payload) {
    const response = payload as { success?: boolean; data?: T; message?: string; errors?: string[] };
    if (response.success === false) {
      throw new ApiError(response.message || "Request failed", 400, response.errors || []);
    }
    return { data: response.data as T, message: response.message || "" };
  }

  return { data: payload as T, message: "" };
}

async function refreshAccessToken() {
  const refreshToken = tokenStore.getRefresh();
  if (!refreshToken) return false;

  const response = await fetch(`${API_BASE_URL}/api/auth/refresh-token`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken })
  });

  if (!response.ok) return false;
  const payload = await response.json();
  const { data } = normalize<{ accessToken: string; refreshToken: string }>(payload);
  if (!data?.accessToken || !data?.refreshToken) return false;
  tokenStore.set(data.accessToken, data.refreshToken);
  return true;
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  headers.set("Accept", "application/json");

  if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const accessToken = tokenStore.getAccess();
  if (!options.skipAuth && accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
    credentials: "include"
  });

  if (response.status === 401 && options.retry !== false && !options.skipAuth) {
    const refreshed = await refreshAccessToken();
    if (refreshed) return apiFetch<T>(path, { ...options, retry: false });
    tokenStore.clear();
    if (typeof window !== "undefined") window.location.href = "/login";
  }

  const text = await response.text();
  const payload = text ? JSON.parse(text) : null;

  if (!response.ok) {
    const message = payload?.message || (response.status === 403 ? "Bạn không có quyền truy cập." : "Có lỗi xảy ra.");
    throw new ApiError(message, response.status, payload?.errors || []);
  }

  return normalize<T>(payload).data;
}

export function toQuery(params: Record<string, string | number | boolean | undefined | null>) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") query.set(key, String(value));
  });
  const value = query.toString();
  return value ? `?${value}` : "";
}

export function unwrapList<T>(value: T[] | { items?: T[]; data?: T[] } | undefined | null): T[] {
  if (!value) return [];
  if (Array.isArray(value)) return value;
  return value.items || value.data || [];
}
