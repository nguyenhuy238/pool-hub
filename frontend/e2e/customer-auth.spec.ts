import { expect, test } from "@playwright/test";

test("customer auth modal closes before navigating to recovery and registration", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: "Đăng nhập khách hàng" }).click();
  await page.getByRole("dialog", { name: "Đăng nhập khách hàng" }).getByRole("link", { name: "Quên mật khẩu?" }).click();

  await expect(page).toHaveURL(/\/forgot-password$/);
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Quên mật khẩu" })).toBeVisible();

  await page.goto("/");
  await page.getByRole("button", { name: "Đăng nhập khách hàng" }).click();
  await page.getByRole("dialog", { name: "Đăng nhập khách hàng" }).getByRole("link", { name: "Tạo tài khoản" }).click();

  await expect(page).toHaveURL(/\/register$/);
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByRole("heading", { name: "Tạo tài khoản" })).toBeVisible();
});

test("customer can request an OTP and reset the password", async ({ page }) => {
  await page.route("**/api/auth/forgot-password", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ success: true, data: {} })
    });
  });
  await page.route("**/api/auth/reset-password", async (route) => {
    expect(route.request().postDataJSON()).toEqual({
      email: "customer@poolhub.com",
      otp: "123456",
      newPassword: "NewPassword@123",
      confirmPassword: "NewPassword@123"
    });
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ success: true, data: {} })
    });
  });

  await page.goto("/forgot-password");
  await page.getByLabel("Email").fill("customer@poolhub.com");
  await page.getByRole("button", { name: "Gửi mã OTP" }).click();

  await expect(page.getByLabel("Mã OTP")).toBeVisible();
  await page.getByLabel("Mã OTP").fill("123456");
  await page.getByLabel("Mật khẩu mới").fill("NewPassword@123");
  await page.getByLabel("Xác nhận mật khẩu").fill("NewPassword@123");
  await page.getByRole("button", { name: "Đặt lại mật khẩu" }).click();

  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole("dialog", { name: "Đăng nhập khách hàng" })).toBeVisible();
});

test("admin login does not expose a customer login link", async ({ page }) => {
  await page.goto("/admin/login");

  await expect(page.getByRole("link", { name: "Đăng nhập bằng tài khoản khách hàng" })).toHaveCount(0);
});
