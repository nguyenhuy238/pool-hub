import { expect, test } from "@playwright/test";

test("customer can open the security page and change the password", async ({ page }) => {
  await page.route("**/api/auth/me", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        success: true,
        data: {
          userId: 101,
          publicId: "customer-101",
          email: "customer@poolhub.com",
          fullName: "Khách hàng",
          roles: ["Customer"],
          permissions: []
        }
      })
    });
  });

  await page.route("**/api/auth/change-password", async (route) => {
    expect(route.request().method()).toBe("PUT");
    expect(route.request().postDataJSON()).toEqual({
      currentPassword: "CurrentPassword@123",
      newPassword: "NewPassword@123",
      confirmPassword: "NewPassword@123"
    });
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ success: true, data: {} })
    });
  });

  await page.route("**/api/auth/logout", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({ success: true, data: {} })
    });
  });

  await page.goto("/customer/change-password");
  await expect(page).toHaveURL(/\/customer\/change-password$/);
  await expect(page.getByRole("link", { name: "Đổi mật khẩu" })).toHaveAttribute("href", "/customer/change-password");
  await expect(page.getByRole("heading", { name: "Đổi mật khẩu" })).toBeVisible();

  await page.getByLabel("Mật khẩu hiện tại").fill("CurrentPassword@123");
  await page.getByLabel(/^Mật khẩu mới/).fill("NewPassword@123");
  await page.getByLabel("Xác nhận mật khẩu mới").fill("NewPassword@123");
  await page.getByRole("button", { name: "Đổi mật khẩu" }).click();

  await expect(page.getByText("Đổi mật khẩu thành công. Vui lòng đăng nhập lại.")).toBeVisible();
  await expect(page).toHaveURL(/\/$/);
});
