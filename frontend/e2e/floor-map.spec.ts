import { expect, test, type APIRequestContext, type Page } from "@playwright/test";

const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5056";
const staffAccount = {
  email: process.env.E2E_STAFF_EMAIL || "staff1@poolhub.com",
  password: process.env.E2E_STAFF_PASSWORD || "Staff@123"
};

async function login(request: APIRequestContext, page: Page) {
  const response = await request.post(`${apiBaseUrl}/api/auth/admin/login`, {
    data: staffAccount
  });
  expect(response.status()).toBe(200);
  const state = await request.storageState();
  await page.context().addCookies(state.cookies);
}

test("staff can load floor map from the real layout endpoint", async ({ page, request }) => {
  const pageErrors: string[] = [];
  const failedApiResponses: string[] = [];
  page.on("pageerror", (error) => pageErrors.push(error.message));
  page.on("response", (response) => {
    if (response.url().startsWith(apiBaseUrl) && response.status() >= 400) {
      failedApiResponses.push(`${response.status()} ${response.url()}`);
    }
  });

  await login(request, page);
  const layoutResponse = page.waitForResponse((response) =>
    response.url() === `${apiBaseUrl}/api/venue-tables/layout`);

  await page.goto("/operation/floor-map");
  const response = await layoutResponse;

  expect(response.status()).toBe(200);
  await expect(page.getByRole("heading", { name: "Sơ đồ bàn vận hành" })).toBeVisible();
  await expect(page.getByText("Endpoint chưa được cấu hình hoặc frontend/backend chưa đồng bộ.")).toHaveCount(0);
  await expect(page.locator(".venue-table-card").first()).toBeVisible();

  await page.getByRole("button", { name: "Làm mới" }).click();
  await expect(page.locator(".venue-table-card").first()).toBeVisible();
  expect(pageErrors).toEqual([]);
  expect(failedApiResponses).toEqual([]);
});
