import { expect, test } from "@playwright/test";
import { landingPathFor, ROLES } from "@/lib/auth/constants";

test("internal accounts land on the shared dashboard", () => {
  expect(landingPathFor([ROLES.ADMIN])).toBe("/dashboard");
  expect(landingPathFor([ROLES.MANAGER])).toBe("/dashboard");
  expect(landingPathFor([ROLES.STAFF])).toBe("/dashboard");
  expect(landingPathFor([ROLES.CASHIER])).toBe("/dashboard");
});

test("customer accounts still land on the customer portal", () => {
  expect(landingPathFor([ROLES.CUSTOMER])).toBe("/customer");
});
