import { apiFetch, toQuery } from "@/lib/api/client";
import type { PagedResult, User } from "@/types";

export type UserQuery = {
  keyword?: string;
  status?: string;
  roleId?: number;
  pageNumber?: number;
  pageSize?: number;
};

export type CreateUserPayload = {
  fullName: string;
  email: string;
  phoneNumber?: string;
  password: string;
  confirmPassword: string;
  roleIds: number[];
};

export type UpdateUserPayload = {
  fullName: string;
  phoneNumber?: string;
  avatarUrl?: string;
  emailConfirmed: boolean;
};

export const userService = {
  getUsers: (params: UserQuery) => apiFetch<PagedResult<User>>(`/api/users${toQuery(params)}`),
  getUserById: (id: number) => apiFetch<User>(`/api/users/${id}`),
  createUser: (payload: CreateUserPayload) =>
    apiFetch<User>("/api/users", { method: "POST", body: JSON.stringify(payload) }),
  updateUser: (id: number, payload: UpdateUserPayload) =>
    apiFetch<User>(`/api/users/${id}`, { method: "PUT", body: JSON.stringify(payload) }),
  updateUserStatus: (id: number, status: "Active" | "Locked" | "Deleted") =>
    apiFetch(`/api/users/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }),
  assignRoles: (userId: number, roleIds: number[]) =>
    apiFetch(`/api/users/${userId}/roles`, { method: "POST", body: JSON.stringify({ roleIds }) }),
  removeRole: (userId: number, roleId: number) =>
    apiFetch(`/api/users/${userId}/roles/${roleId}`, { method: "DELETE" })
};
