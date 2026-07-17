import { expect, test } from "@playwright/test";

test("landing review section does not show the long form by default", async ({ page }) => {
  await page.route("**/api/public/reviews**", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        success: true,
        data: {
          items: [
            {
              publicId: "review-1",
              rating: 5,
              content: "Bàn tốt, nhân viên hỗ trợ nhanh.",
              displayName: "Nguyen Q. Huy",
              isVerified: true
            }
          ],
          totalItems: 1,
          pageNumber: 1,
          pageSize: 6,
          averageRating: 5,
          ratingDistribution: { 1: 0, 2: 0, 3: 0, 4: 0, 5: 1 }
        }
      })
    });
  });

  await page.goto("/");

  await expect(page.getByRole("button", { name: "Bạn đã chơi tại PoolHub? Gửi đánh giá" })).toBeVisible();
  await expect(page.getByLabel("Số điện thoại")).toHaveCount(0);
  await expect(page.getByLabel("Loại mã")).toHaveCount(0);

  await page.getByRole("button", { name: "Bạn đã chơi tại PoolHub? Gửi đánh giá" }).click();
  await expect(page.getByLabel("Mã hóa đơn, phiên chơi hoặc booking")).toBeVisible();
  await expect(page.getByLabel("5 sao")).toBeVisible();
  await expect(page.getByLabel("Đăng ẩn danh")).toBeVisible();
});

test("review invitation page submits rating with optional anonymous feedback", async ({ page }) => {
  await page.route("**/api/public/reviews/invitations/token-1", async (route) => {
    await route.fulfill({
      contentType: "application/json",
      body: JSON.stringify({
        success: true,
        data: {
          publicId: "invitation-1",
          customerDisplayName: "Nguyen Quoc Huy",
          invoiceCode: "INV-1",
          sessionCode: "SS-1",
          canSubmit: true
        }
      })
    });
  });
  await page.route("**/api/public/reviews/invitations/token-1/submit", async (route) => {
    const body = route.request().postDataJSON();
    expect(body.rating).toBe(5);
    expect(body.isAnonymous).toBe(true);
    expect(body.content).toBeUndefined();
    await route.fulfill({ contentType: "application/json", body: JSON.stringify({ success: true, data: { publicId: "review-1" } }) });
  });

  await page.goto("/reviews/new?token=token-1");

  await page.getByLabel("5 sao").click();
  await page.getByLabel("Đăng ẩn danh").check();
  await page.getByRole("button", { name: "Gửi đánh giá" }).click();

  await expect(page.getByText("Cảm ơn bạn đã chia sẻ trải nghiệm tại PoolHub.")).toBeVisible();
});
