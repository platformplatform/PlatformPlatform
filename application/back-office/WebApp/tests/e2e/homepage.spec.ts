import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  test("Navigate to back-office & verify homepage loads correctly", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Navigate to back-office & verify homepage displays welcome message")(async () => {
      await ownerPage.goto("/back-office");

      await expect(ownerPage).toHaveURL("/back-office");
      await expect(ownerPage.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();
      await expect(ownerPage.getByText("Manage tenants, view system data")).toBeVisible();
    })();
  });
});
