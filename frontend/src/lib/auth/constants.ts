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
  USERS_MANAGE: "users.manage",
  ROLES_MANAGE: "roles.manage",
  CUSTOMERS_MANAGE: "customers.manage",
  VENUE_MANAGE: "venue.manage",
  PRICING_MANAGE: "pricing.manage",
  PRODUCTS_MANAGE: "products.manage",
  DISCOUNTS_MANAGE: "discounts.manage",
  LANDING_MANAGE: "landing.manage",
  REPORTS_VIEW: "reports.view",
  PAYMENTS_MANAGE: "payments.manage",
  INVENTORY_MANAGE: "inventory.manage",
  AUDIT_VIEW: "audit.view"
} as const;

export const ADMIN_ROLES: RoleName[] = [ROLES.ADMIN];
export const MANAGEMENT_READ_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER];
export const OPERATION_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER, ROLES.STAFF, ROLES.CASHIER];
export const INTERNAL_LOGIN_ROLES: RoleName[] = [...OPERATION_ROLES];
export const CUSTOMER_LOGIN_ROLES: RoleName[] = [ROLES.CUSTOMER];
export const DASHBOARD_ROLES: RoleName[] = [...OPERATION_ROLES];

export function landingPathFor(roles: RoleName[]) {
  if (roles.some((role) => CUSTOMER_LOGIN_ROLES.includes(role))) return "/customer";
  if (roles.some((role) => INTERNAL_LOGIN_ROLES.includes(role))) return "/dashboard";
  return "/";
}
