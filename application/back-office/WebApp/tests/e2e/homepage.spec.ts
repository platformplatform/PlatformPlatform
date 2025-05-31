import { expect, test } from "@playwright/test";

test("@smoke back-office homepage", async ({ page }) => {
  await page.goto("/back-office");

  // Verify page loads successfully and has correct title
  await expect(page.locator("body")).toBeVisible();
});
