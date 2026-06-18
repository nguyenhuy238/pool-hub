import type { RoleName } from "@/types";

export const ROLES = {
  ADMIN: "Admin",
  MANAGER: "Manager",
  STAFF: "Staff",
  CASHIER: "Cashier",
  CUSTOMER: "Customer"
} as const satisfies Record<string, RoleName>;

export const ADMIN_ROLES: RoleName[] = [ROLES.ADMIN];
export const MANAGEMENT_READ_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER];
export const OPERATION_ROLES: RoleName[] = [ROLES.ADMIN, ROLES.MANAGER, ROLES.STAFF, ROLES.CASHIER];

export function landingPathFor(roles: RoleName[]) {
  if (roles.includes(ROLES.ADMIN)) return "/admin/dashboard";
  if (roles.includes(ROLES.MANAGER)) return "/dashboard";
  if (roles.some((role) => [ROLES.STAFF, ROLES.CASHIER].includes(role as typeof ROLES.STAFF | typeof ROLES.CASHIER))) {
    return "/operation/floor-map";
  }
  return "/booking";
}
