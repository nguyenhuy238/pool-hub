import { apiFetch } from "@/lib/api/client";
import type { 
  DashboardSummary, 
  RevenueReport, 
  TableUsageReport, 
  ProductSalesReport, 
  BookingReport 
} from "@/types";

export type ReportQueryRequest = {
  fromDate?: string;
  toDate?: string;
};

export const dashboardService = {
  getStats: () =>
    apiFetch<DashboardSummary>("/api/dashboard/summary"),

  getRevenueReport: (params?: ReportQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.fromDate) query.append("fromDate", params.fromDate);
    if (params?.toDate) query.append("toDate", params.toDate);
    return apiFetch<RevenueReport[]>(`/api/reports/revenue?${query.toString()}`);
  },

  getTableUsageReport: (params?: ReportQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.fromDate) query.append("fromDate", params.fromDate);
    if (params?.toDate) query.append("toDate", params.toDate);
    return apiFetch<TableUsageReport[]>(`/api/reports/table-usage?${query.toString()}`);
  },

  getProductReport: (params?: ReportQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.fromDate) query.append("fromDate", params.fromDate);
    if (params?.toDate) query.append("toDate", params.toDate);
    return apiFetch<ProductSalesReport[]>(`/api/reports/products?${query.toString()}`);
  },

  getBookingReport: (params?: ReportQueryRequest) => {
    const query = new URLSearchParams();
    if (params?.fromDate) query.append("fromDate", params.fromDate);
    if (params?.toDate) query.append("toDate", params.toDate);
    return apiFetch<BookingReport[]>(`/api/reports/bookings?${query.toString()}`);
  },
};
