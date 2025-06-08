import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { assertNoUnexpectedErrors, createTestContext } from "@shared/e2e/utils/test-assertions";

test("@smoke back-office homepage", async ({ ownerPage }) => {
  const context = createTestContext(ownerPage);

  // Act & Assert: Navigate to back-office & verify it loads correctly
  await ownerPage.goto("/back-office");
  await expect(ownerPage.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();

  // Assert: Assert no unexpected errors occurred
  assertNoUnexpectedErrors(context);
});
