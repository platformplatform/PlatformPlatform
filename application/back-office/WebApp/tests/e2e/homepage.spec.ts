import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertNoUnexpectedErrors, createTestContext } from "@shared/e2e/utils/test-assertions";

test("@smoke back-office homepage", async ({ ownerPage }) => {
  const context = createTestContext(ownerPage);

  // Step 1: Navigate to back-office and verify it loads
  await ownerPage.goto("/back-office");
  await expect(ownerPage.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();

  // Step 3: Assert no unexpected errors occurred
  assertNoUnexpectedErrors(context);
});
