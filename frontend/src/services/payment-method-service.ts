import { apiFetch } from "@/lib/api/client";
import type { PaymentMethod } from "@/types";

export type UpsertPaymentMethodRequest = {
  name: string;
  code: string;
  description?: string;
  isActive: boolean;
};

export const paymentMethodService = {
  getPaymentMethods: () =>
    apiFetch<PaymentMethod[]>("/api/payment-methods"),

  createPaymentMethod: (payload: UpsertPaymentMethodRequest) =>
    apiFetch<PaymentMethod>("/api/payment-methods", { method: "POST", body: JSON.stringify(payload) }),

  updatePaymentMethod: (id: number, payload: UpsertPaymentMethodRequest) =>
    apiFetch<PaymentMethod>(`/api/payment-methods/${id}`, { method: "PUT", body: JSON.stringify(payload) }),

  updateStatus: (id: number, isActive: boolean) =>
    apiFetch(`/api/payment-methods/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) }),
};
