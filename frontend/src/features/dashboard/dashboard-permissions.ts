import { PERMISSIONS, ROLES } from "@/lib/auth/constants";
import type { RoleName } from "@/types";

export function hasRole(roles: RoleName[], role: string) {
  return roles.some((item) => item === role);
}

export function hasAnyRole(roles: RoleName[], allowed: string[]) {
  return roles.some((role) => allowed.includes(role));
}

export function hasPermission(permissions: string[], permission: string) {
  return permissions.includes(permission);
}

export function canViewReports(roles: RoleName[], permissions: string[]) {
  return hasAnyRole(roles, [ROLES.ADMIN, ROLES.MANAGER]) || hasPermission(permissions, PERMISSIONS.REPORTS_VIEW);
}

export function canViewPayments(roles: RoleName[], permissions: string[]) {
  return hasAnyRole(roles, [ROLES.ADMIN, ROLES.MANAGER, ROLES.STAFF]) || hasPermission(permissions, PERMISSIONS.PAYMENTS_MANAGE);
}

export function canViewInventory(roles: RoleName[], permissions: string[]) {
  return hasAnyRole(roles, [ROLES.ADMIN, ROLES.MANAGER]) || hasPermission(permissions, PERMISSIONS.INVENTORY_MANAGE);
}

export function canViewAudit(roles: RoleName[], permissions: string[]) {
  return hasRole(roles, ROLES.ADMIN) || hasPermission(permissions, PERMISSIONS.AUDIT_VIEW);
}
