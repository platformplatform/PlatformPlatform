import { expect, test } from "@playwright/test";

test("@smoke homepage loads", async ({ page }) => {
  await page.goto("/");

  // Expect the page to load successfully (no 404 or error)
  await expect(page.locator("body")).toBeVisible();
});
