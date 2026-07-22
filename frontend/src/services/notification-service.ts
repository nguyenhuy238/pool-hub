import { apiFetch, toQuery } from "@/lib/api/client";
import type { Notification, PagedResult } from "@/types";

export type NotificationQueryRequest = {
  isRead?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export type CreateNotificationRequest = {
  userId?: number;
  title: string;
  message: string;
};

export const notificationService = {
  getNotifications: (params: NotificationQueryRequest = {}) =>
    apiFetch<PagedResult<Notification>>(`/api/notifications${toQuery(params)}`),

  getNotificationById: (id: number) =>
    apiFetch<Notification>(`/api/notifications/${id}`),

  createNotification: (payload: CreateNotificationRequest) =>
    apiFetch<Notification>("/api/notifications", { method: "POST", body: JSON.stringify(payload) }),

  markAsRead: (id: number) =>
    apiFetch(`/api/notifications/${id}/read`, { method: "PATCH" }),

  markAllAsRead: () =>
    apiFetch("/api/notifications/read-all", { method: "PATCH" }),

  getUnreadCount: () =>
    apiFetch<{ count: number }>("/api/notifications/unread-count"),

  deleteNotification: (id: number) =>
    apiFetch(`/api/notifications/${id}`, { method: "DELETE" }),
};
