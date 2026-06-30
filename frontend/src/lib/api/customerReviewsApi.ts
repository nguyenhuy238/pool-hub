import { apiFetch, toQuery } from "@/lib/api/client";
import type { CustomerReview, PagedResult, PublicReview, ReviewInvitation, ReviewInvitationLink } from "@/types";

export type CreatePublicReviewPayload = {
  bookingCode?: string;
  sessionCode?: string;
  invoiceCode?: string;
  phoneNumber: string;
  fullName?: string;
  rating: number;
  content: string;
  avatarUrl?: string;
  checkInImageUrl?: string;
};

export const customerReviewsApi = {
  publicList: (params: Record<string, string | number | boolean | null | undefined> = {}) =>
    apiFetch<PagedResult<PublicReview>>(`/api/public/reviews${toQuery(params)}`, { skipAuth: true }),
  publicCreate: (body: CreatePublicReviewPayload) =>
    apiFetch<CustomerReview>("/api/public/reviews", { method: "POST", body: JSON.stringify(body), skipAuth: true }),
  invitation: (token: string) =>
    apiFetch<ReviewInvitation>(`/api/public/reviews/invitations/${encodeURIComponent(token)}`, { skipAuth: true }),
  submitInvitation: (token: string, body: { rating: number; content: string; displayName?: string; avatarUrl?: string; checkInImageUrl?: string }) =>
    apiFetch<CustomerReview>(`/api/public/reviews/invitations/${encodeURIComponent(token)}/submit`, { method: "POST", body: JSON.stringify(body), skipAuth: true }),
  list: (params: Record<string, string | number | boolean | null | undefined> = {}) =>
    apiFetch<PagedResult<CustomerReview>>(`/api/customer-reviews${toQuery(params)}`),
  approve: (publicId: string, body: { isFeatured?: boolean; displayOrder?: number } = {}) =>
    apiFetch<CustomerReview>(`/api/customer-reviews/${publicId}/approve`, { method: "PATCH", body: JSON.stringify(body) }),
  reject: (publicId: string, reason: string) =>
    apiFetch<CustomerReview>(`/api/customer-reviews/${publicId}/reject`, { method: "PATCH", body: JSON.stringify({ reason }) }),
  visibility: (publicId: string, body: { status: number; isFeatured?: boolean; displayOrder?: number }) =>
    apiFetch<CustomerReview>(`/api/customer-reviews/${publicId}/visibility`, { method: "PATCH", body: JSON.stringify(body) }),
  update: (publicId: string, body: Partial<CustomerReview>) =>
    apiFetch<CustomerReview>(`/api/customer-reviews/${publicId}`, { method: "PUT", body: JSON.stringify(body) }),
  hide: (publicId: string) =>
    apiFetch<Record<string, never>>(`/api/customer-reviews/${publicId}`, { method: "DELETE" }),
  createInvitationForInvoice: (invoiceId: number) =>
    apiFetch<ReviewInvitationLink>(`/api/customer-reviews/invitations/invoice/${invoiceId}`, { method: "POST" })
};
