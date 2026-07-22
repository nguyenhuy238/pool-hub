import { expect, request, test, type Page } from "@playwright/test";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";

const accounts = {
  admin: {
    email: process.env.E2E_ADMIN_EMAIL || "admin@poolhub.com",
    password: process.env.E2E_ADMIN_PASSWORD || "Admin@123"
  },
  manager: {
    email: process.env.E2E_MANAGER_EMAIL || "manager@poolhub.com",
    password: process.env.E2E_MANAGER_PASSWORD || "Manager@123"
  },
  staff: {
    email: process.env.E2E_STAFF_EMAIL || "staff1@poolhub.com",
    password: process.env.E2E_STAFF_PASSWORD || "Staff@123"
  },
  cashier: {
    email: process.env.E2E_CASHIER_EMAIL || "cashier@poolhub.com",
    password: process.env.E2E_CASHIER_PASSWORD || "Cashier@123"
  }
};

async function assertBackendReady() {
  const context = await request.newContext({ baseURL: apiBaseUrl });
  const response = await context.get("/api/public/landing-page", { timeout: 10_000 });
  await context.dispose();
  expect(response.ok(), `Backend is not ready at ${apiBaseUrl}. Start PoolHub.API before running E2E tests.`).toBeTruthy();
}

async function login(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Mật khẩu").fill(password);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  await expect(page.locator(".app-shell")).toBeVisible();
}

async function expectAllowed(page: Page, path: string) {
  await page.goto(path);
  await expect(page).not.toHaveURL(/\/login|\/403/);
  await expect(page.locator("main")).toBeVisible();
}

async function expectForbidden(page: Page, path: string) {
  await page.goto(path);
  await expect(page).toHaveURL(/\/403/);
  await expect(page.getByText("Bạn không có quyền truy cập")).toBeVisible();
}

test.beforeAll(assertBackendReady);

test.describe("role route guard smoke", () => {
  test("admin can access admin and management routes", async ({ page }) => {
    await login(page, accounts.admin.email, accounts.admin.password);
    for (const path of [
      "/admin/dashboard",
      "/admin/analytics",
      "/admin/users",
      "/admin/roles",
      "/admin/audit-logs",
      "/management/floors",
      "/management/products",
      "/admin/reports"
    ]) {
      await expectAllowed(page, path);
    }
  });

  test("manager can access management, audit, reports but not users or roles", async ({ page }) => {
    await login(page, accounts.manager.email, accounts.manager.password);
    for (const path of ["/dashboard", "/management/floors", "/management/products", "/admin/audit-logs", "/admin/reports", "/admin/analytics"]) {
      await expectAllowed(page, path);
    }
    await expectForbidden(page, "/admin/users");
    await expectForbidden(page, "/admin/roles");
  });

  test("staff can access operation routes but not admin or management", async ({ page }) => {
    await login(page, accounts.staff.email, accounts.staff.password);
    for (const path of ["/operation/floor-map", "/operation/bookings", "/operation/sessions", "/operation/orders"]) {
      await expectAllowed(page, path);
    }
    await expectForbidden(page, "/admin/users");
    await expectForbidden(page, "/management/floors");
  });

  test("cashier can access invoice/payment routes but not admin-only or management", async ({ page }) => {
    await login(page, accounts.cashier.email, accounts.cashier.password);
    for (const path of ["/operation/invoices", "/admin/payments"]) {
      await expectAllowed(page, path);
    }
    await expectForbidden(page, "/admin/users");
    await expectForbidden(page, "/management/products");
  });

  test("public pages work and protected routes redirect to login", async ({ page }) => {
    await page.goto("/");
    await expect(page.locator("body")).toBeVisible();

    await page.goto("/booking");
    await expect(page.locator("body")).toBeVisible();

    await page.goto("/admin/dashboard");
    await expect(page).toHaveURL(/\/login/);
  });
});
