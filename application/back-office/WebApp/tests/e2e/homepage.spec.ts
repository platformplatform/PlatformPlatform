import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";

test("@smoke back-office homepage", async ({ ownerPage }) => {
  // Act & Assert: Navigate to back-office & verify it loads correctly
  await ownerPage.goto("/");
  await ownerPage.goto("/back-office");
  await expect(ownerPage).toHaveURL("/back-office");
  await expect(ownerPage.getByRole("heading", { name: "Welcome to the Back Office" })).toBeVisible();

  await expect(ownerPage.getByText("Manage tenants, view system data")).toBeVisible();
});
