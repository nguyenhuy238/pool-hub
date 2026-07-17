import type { RoleName } from "@/types";

export const ROLES = {
  ADMIN: "Admin",
  MANAGER: "Manager",
  STAFF: "Staff",
  CASHIER: "Cashier",
  CUSTOMER: "Customer",
  GUEST: "Guest"
} as const satisfies Record<string, RoleName>;

export const PERMISSIONS = {
  REPORTS_VIEW: "reports.view",
  PAYMENTS_MANAGE: "payments.manage",
  INVENTORY_MANAGE: "inventory.manage",
  AUDIT_VIEW: "audit.view"
} as const;

export const ADMIN_ROLES: RoleName[] = [ROLES.ADMIN];
export const MANAGEMENT_READ_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER];
export const OPERATION_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER, ROLES.STAFF, ROLES.CASHIER];
export const DASHBOARD_ROLES: RoleName[] = [...OPERATION_ROLES, ROLES.CUSTOMER];

export function landingPathFor(roles: RoleName[]) {
  if (roles.some((role) => DASHBOARD_ROLES.includes(role))) return "/dashboard";
  if (roles.some((role) => [ROLES.STAFF, ROLES.CASHIER].includes(role as typeof ROLES.STAFF | typeof ROLES.CASHIER))) {
    return "/operation/floor-map";
  }
  return "/booking";
}
