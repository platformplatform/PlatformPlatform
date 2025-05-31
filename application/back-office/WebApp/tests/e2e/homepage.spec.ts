import { expect, test } from "@playwright/test";
import { assertNoUnexpectedErrors, createTestContext } from "../../../../shared-webapp/tests/e2e/utils/test-assertions";
import { getVerificationCode, testUser } from "../../../../shared-webapp/tests/e2e/utils/test-data";

test("@smoke back-office homepage", async ({ page }) => {
  const context = createTestContext(page);
  const user = testUser();

  // Step 1: Create a user account first through signup flow
  await page.goto("/");
  await page.getByRole("button", { name: "Get started today" }).first().click();
  await page.getByRole("textbox", { name: "Email" }).fill(user.email);
  await page.getByRole("button", { name: "Create your account" }).click();
  await expect(page).toHaveURL("/signup/verify");
  await page.keyboard.type(getVerificationCode());
  await page.getByRole("button", { name: "Verify" }).click();
  await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
  await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

  // Step 2: Navigate to back-office and verify it loads
  await page.goto("/back-office");
  await expect(page.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();

  // Step 3: Assert no unexpected errors occurred
  assertNoUnexpectedErrors(context);
});
