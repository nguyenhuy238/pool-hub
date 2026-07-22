import { apiFetch } from "@/lib/api/client";
import type { Invoice, PagedResult, PaymentMethod } from "@/types";

export type InvoiceQueryRequest = {
  customerId?: number;
  sessionId?: number;
  paymentStatus?: number;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
};

export type InvoiceDetailDto = Invoice & {
  lines: unknown[];
  discounts: unknown[];
  payments: unknown[];
};

export type CreatePaymentRequest = {
  invoiceId: number;
  paymentMethodId: number;
  amount: number;
  transactionCode?: string;
  note?: string;
};

export type ApplyDiscountRequest = {
  discountCode: string;
};

export const invoiceService = {
  getInvoices: (params?: InvoiceQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.customerId) query.append("customerId", params.customerId.toString());
    if (params?.sessionId) query.append("sessionId", params.sessionId.toString());
    if (params?.paymentStatus !== undefined) query.append("paymentStatus", params.paymentStatus.toString());
    if (params?.fromDate) query.append("fromDate", params.fromDate);
    if (params?.toDate) query.append("toDate", params.toDate);
    if (params?.pageNumber) query.append("pageNumber", params.pageNumber.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    
    return apiFetch<PagedResult<Invoice>>(`/api/invoices?${query.toString()}`);
  },

  getInvoiceById: (id: number) =>
    apiFetch<InvoiceDetailDto>(`/api/invoices/${id}`),

  getPaymentMethods: () =>
    apiFetch<PaymentMethod[]>("/api/invoices/payment-methods"),

  generateFromSession: (sessionId: number) =>
    apiFetch<Invoice>(`/api/invoices/generate/${sessionId}`, { method: "POST" }),

  createPayment: (payload: CreatePaymentRequest) =>
    apiFetch("/api/invoices/payments", { method: "POST", body: JSON.stringify(payload) }),

  applyDiscount: (id: number, payload: ApplyDiscountRequest) =>
    apiFetch(`/api/invoices/${id}/discounts`, { method: "POST", body: JSON.stringify(payload) }),

  removeDiscount: (id: number) =>
    apiFetch(`/api/invoices/${id}/discounts`, { method: "DELETE" }),
};
