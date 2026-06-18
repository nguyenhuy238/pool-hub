import { apiFetch, toQuery } from "@/lib/api/client";
import type { AuditLog, PagedResult } from "@/types";

export type AuditQuery = {
  actorUserId?: number;
  action?: string;
  entityName?: string;
  fromDate?: string;
  toDate?: string;
  pageNumber?: number;
  pageSize?: number;
};

export const auditService = {
  getAuditLogs: (params: AuditQuery) =>
    apiFetch<PagedResult<AuditLog>>(`/api/audit-logs${toQuery(params)}`),
  getAuditLogById: (id: number) => apiFetch<AuditLog>(`/api/audit-logs/${id}`)
};
