import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import {
  blurActiveElement,
  createTestContext,
  expectToastMessage,
  selectOption
} from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  /**
   * OWNER CONTACT INFORMATION WORKFLOW
   *
   * Tests the complete contact information lifecycle on the account settings page:
   * - Contact information section visibility for Owner
   * - Empty state with "Not provided" placeholders
   * - Edit dialog opens with correct fields
   * - Fill in all contact info fields (phone, address, city, postal code, country)
   * - Save and verify success toast
   * - Read-only summary displays saved values correctly
   * - Re-edit contact info and update a field
   * - Verify updated values reflected in summary
   */
  test("should handle owner contact info create, view & edit workflow", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // === CONTACT INFO SECTION VISIBILITY ===
    await step("Navigate to account settings & verify contact information section with empty state")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByRole("heading", { name: "Account settings" })).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Contact information" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Edit" })).toBeVisible();
      await expect(ownerPage.getByText("Phone")).toBeVisible();
      await expect(ownerPage.getByText("Not provided").first()).toBeVisible();
    })();

    // === CREATE CONTACT INFO ===
    await step("Click Edit & fill in all contact info fields & save successfully")(async () => {
      await ownerPage.getByRole("button", { name: "Edit" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Edit contact information" })).toBeVisible();
      await expect(ownerPage.getByLabel("Phone number")).toBeVisible();
      await expect(ownerPage.getByLabel("Address")).toBeVisible();
      await expect(ownerPage.getByLabel("Postal code")).toBeVisible();
      await expect(ownerPage.getByLabel("City")).toBeVisible();

      await ownerPage.getByLabel("Phone number").fill("+45 12345678");
      await ownerPage.getByLabel("Address").fill("Vestergade 12");
      await ownerPage.getByLabel("Postal code").fill("1456");
      await ownerPage.getByLabel("City").fill("Copenhagen");
      await selectOption(ownerPage.getByLabel("Country"), ownerPage, "Denmark");
      await blurActiveElement(ownerPage);

      await ownerPage.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Contact information updated");
    })();

    // === VERIFY SAVED VALUES ===
    await step("Navigate to settings & verify contact info displays saved values")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByText("+45 12345678")).toBeVisible();
      await expect(ownerPage.getByText("Vestergade 12")).toBeVisible();
      await expect(ownerPage.getByText("1456")).toBeVisible();
      await expect(ownerPage.getByText("Copenhagen")).toBeVisible();
      await expect(ownerPage.getByText("Denmark")).toBeVisible();
    })();

    // === EDIT CONTACT INFO ===
    await step("Click Edit & update phone number & save successfully")(async () => {
      await ownerPage.getByRole("button", { name: "Edit" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Edit contact information" })).toBeVisible();
      await expect(ownerPage.getByLabel("Phone number")).toHaveValue("+45 12345678");
      await expect(ownerPage.getByLabel("Address")).toHaveValue("Vestergade 12");
      await expect(ownerPage.getByLabel("Postal code")).toHaveValue("1456");
      await expect(ownerPage.getByLabel("City")).toHaveValue("Copenhagen");

      await ownerPage.getByLabel("Phone number").fill("+45 87654321");
      await blurActiveElement(ownerPage);

      await ownerPage.getByRole("button", { name: "Save changes" }).click();

      await expectToastMessage(context, "Contact information updated");
    })();

    // === VERIFY UPDATED VALUES ===
    await step("Navigate to settings & verify updated phone number in summary")(async () => {
      await ownerPage.goto("/account/settings");

      await expect(ownerPage.getByText("+45 87654321")).toBeVisible();
      await expect(ownerPage.getByText("Vestergade 12")).toBeVisible();
      await expect(ownerPage.getByText("Copenhagen")).toBeVisible();
      await expect(ownerPage.getByText("Denmark")).toBeVisible();
    })();
  });
});
