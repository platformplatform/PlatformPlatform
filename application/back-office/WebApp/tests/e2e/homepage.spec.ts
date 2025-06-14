import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { step } from "@shared/e2e/utils/step-decorator";

test("@smoke back-office homepage", async ({ ownerPage }) => {
  await step("Navigate to back-office & verify homepage loads correctly")(async () => {
    await ownerPage.goto("/");
    await ownerPage.goto("/back-office");

    await expect(ownerPage).toHaveURL("/back-office");
    await expect(ownerPage.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();
    await expect(ownerPage.getByText("Manage tenants, view system data")).toBeVisible();
  })();
});
