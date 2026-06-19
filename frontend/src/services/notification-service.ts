import { apiFetch } from "@/lib/api/client";
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
  getNotifications: (params?: NotificationQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.isRead !== undefined) query.append("isRead", params.isRead.toString());
    if (params?.pageNumber) query.append("pageNumber", params.pageNumber.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    
    return apiFetch<PagedResult<Notification>>(`/api/notifications?${query.toString()}`);
  },

  getNotificationById: (id: number) =>
    apiFetch<Notification>(`/api/notifications/${id}`),

  createNotification: (payload: CreateNotificationRequest) =>
    apiFetch<Notification>("/api/notifications", { method: "POST", body: JSON.stringify(payload) }),

  markAsRead: (id: number) =>
    apiFetch(`/api/notifications/${id}/read`, { method: "PATCH" }),

  markAllAsRead: () =>
    apiFetch("/api/notifications/read-all", { method: "PATCH" }),
};
