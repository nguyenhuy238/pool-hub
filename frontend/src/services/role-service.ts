import { apiFetch, toQuery } from "@/lib/api/client";
import type { PagedResult, Role } from "@/types";

export type RolePayload = { name: string; description?: string; isSystem?: boolean };
export type Permission = { permissionId: number; code: string; name: string; group: string; description?: string };

export const roleService = {
  getRoles: (params: { keyword?: string; pageNumber?: number; pageSize?: number } = {}) =>
    apiFetch<PagedResult<Role>>(`/api/roles${toQuery(params)}`),
  getRoleById: (id: number) => apiFetch<Role>(`/api/roles/${id}`),
  createRole: (payload: RolePayload) =>
    apiFetch<Role>("/api/roles", { method: "POST", body: JSON.stringify(payload) }),
  updateRole: (id: number, payload: RolePayload) =>
    apiFetch<Role>(`/api/roles/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  deleteRole: (id: number) => apiFetch(`/api/roles/${id}`, { method: "DELETE" })
  ,
  getPermissions: () => apiFetch<Permission[]>("/api/roles/permissions"),
  setPermissions: (id: number, permissionIds: number[]) =>
    apiFetch(`/api/roles/${id}/permissions`, { method: "PUT", body: JSON.stringify({ permissionIds }) })
};
