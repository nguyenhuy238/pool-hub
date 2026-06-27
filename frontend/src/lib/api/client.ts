"use client";

export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";

export class ApiError extends Error {
  status: number;
  errors: string[];

  constructor(message: string, status: number, errors: string[] = []) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
}

type RequestOptions = RequestInit & {
  skipAuth?: boolean;
  retry?: boolean;
  timeoutMs?: number;
};

const tokenKey = "poolhub.accessToken";
const refreshKey = "poolhub.refreshToken";
let refreshPromise: Promise<boolean> | null = null;

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

async function executeRefresh() {
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

export async function refreshAccessToken() {
  if (!refreshPromise) {
    refreshPromise = executeRefresh().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

function notifyAuthInvalid() {
  tokenStore.clear();
  if (typeof window !== "undefined") {
    window.dispatchEvent(new Event("poolhub:auth-invalid"));
  }
}

export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers = new Headers(options.headers);
  const timeoutController = options.timeoutMs ? new AbortController() : null;
  const timeoutId = timeoutController
    ? window.setTimeout(() => timeoutController.abort(), options.timeoutMs)
    : null;
  const abortFromCaller = () => timeoutController?.abort();

  if (options.signal) {
    if (options.signal.aborted) abortFromCaller();
    else options.signal.addEventListener("abort", abortFromCaller, { once: true });
  }

  headers.set("Accept", "application/json");

  if (options.body && !(options.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const accessToken = tokenStore.getAccess();
  if (!options.skipAuth && accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers,
      signal: timeoutController?.signal ?? options.signal,
      credentials: "include"
    });
  } catch (error) {
    if (timeoutController?.signal.aborted && !options.signal?.aborted) {
      throw new ApiError("Backend phản hồi quá lâu. Vui lòng thử lại.", 408);
    }
    throw error;
  } finally {
    if (timeoutId !== null) window.clearTimeout(timeoutId);
    options.signal?.removeEventListener("abort", abortFromCaller);
  }

  if (response.status === 401 && options.retry !== false && !options.skipAuth) {
    const refreshed = await refreshAccessToken();
    if (refreshed) return apiFetch<T>(path, { ...options, retry: false });
    notifyAuthInvalid();
  }

  const text = await response.text();
  let payload: { message?: string; errors?: string[] } | null = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = null;
    }
  }

  if (!response.ok) {
    const fallback: Record<number, string> = {
      400: "Dữ liệu gửi lên không hợp lệ.",
      403: "Bạn không có quyền truy cập.",
      404: "API không tồn tại hoặc backend chưa được cập nhật.",
      409: "Dữ liệu bị xung đột.",
      500: "Backend gặp lỗi khi xử lý dữ liệu.",
      503: "Không thể kết nối dịch vụ hoặc cơ sở dữ liệu."
    };
    const message = payload?.message || fallback[response.status] || `Request thất bại (${response.status}).`;
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

export function getTotalPages(value: unknown, fallbackPageSize = 20) {
  if (!value || Array.isArray(value) || typeof value !== "object") return 1;

  const page = value as { totalPages?: unknown; totalItems?: unknown; totalCount?: unknown; pageSize?: unknown };
  const explicitTotalPages = Number(page.totalPages);
  if (Number.isFinite(explicitTotalPages) && explicitTotalPages > 0) return explicitTotalPages;

  const totalItems = Number(page.totalItems ?? page.totalCount);
  const pageSize = Number(page.pageSize ?? fallbackPageSize);
  if (!Number.isFinite(totalItems) || totalItems <= 0 || !Number.isFinite(pageSize) || pageSize <= 0) return 1;

  return Math.max(1, Math.ceil(totalItems / pageSize));
}
