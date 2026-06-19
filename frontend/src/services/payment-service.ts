import { apiFetch } from "@/lib/api/client";
import type { Payment, PagedResult } from "@/types";

export type PaymentQueryRequest = {
  invoiceId?: number;
  paymentMethodId?: number;
  paymentStatus?: number;
  date?: string;
  pageNumber?: number;
  pageSize?: number;
};

export type CreatePaymentRequest = {
  invoiceId: number;
  paymentMethodId: number;
  amount: number;
  transactionCode?: string;
  note?: string;
};

export const paymentService = {
  getPayments: (params?: PaymentQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.invoiceId) query.append("invoiceId", params.invoiceId.toString());
    if (params?.paymentMethodId) query.append("paymentMethodId", params.paymentMethodId.toString());
    if (params?.paymentStatus !== undefined) query.append("paymentStatus", params.paymentStatus.toString());
    if (params?.date) query.append("date", params.date);
    if (params?.pageNumber) query.append("pageNumber", params.pageNumber.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    
    return apiFetch<PagedResult<Payment>>(`/api/payments?${query.toString()}`);
  },

  createPayment: (payload: CreatePaymentRequest) =>
    apiFetch("/api/payments", { method: "POST", body: JSON.stringify(payload) }),
};
