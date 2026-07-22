import { apiFetch } from "@/lib/api/client";
import type { Discount, PagedResult } from "@/types";

export type DiscountQueryRequest = {
  search?: string;
  isActive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export type UpsertDiscountRequest = {
  discountCode: string;
  name: string;
  discountType: string;
  value: number;
  maxAmount?: number;
  minTimeSubtotal?: number;
  startsAtUtc: string;
  endsAtUtc?: string;
  isActive: boolean;
};

export type ValidateDiscountRequest = {
  discountCode: string;
  timeSubtotal: number;
};

export type DiscountValidationDto = {
  isValid: boolean;
  message: string;
  discountAmount?: number;
};

export const discountService = {
  getDiscounts: (params?: DiscountQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.search) query.append("search", params.search);
    if (params?.isActive !== undefined) query.append("isActive", params.isActive.toString());
    if (params?.pageNumber) query.append("pageNumber", params.pageNumber.toString());
    if (params?.pageSize) query.append("pageSize", params.pageSize.toString());
    
    return apiFetch<PagedResult<Discount>>(`/api/discounts?${query.toString()}`);
  },

  createDiscount: (payload: UpsertDiscountRequest) =>
    apiFetch<Discount>("/api/discounts", { method: "POST", body: JSON.stringify(payload) }),

  updateDiscount: (id: number, payload: UpsertDiscountRequest) =>
    apiFetch<Discount>(`/api/discounts/${id}`, { method: "PUT", body: JSON.stringify(payload) }),

  updateStatus: (id: number, isActive: boolean) =>
    apiFetch(`/api/discounts/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) }),

  validateDiscount: (payload: ValidateDiscountRequest) =>
    apiFetch<DiscountValidationDto>("/api/discounts/validate", { method: "POST", body: JSON.stringify(payload) }),
};
