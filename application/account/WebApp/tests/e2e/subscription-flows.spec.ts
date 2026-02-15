import { expect, type Page } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

const MOCK_PROVIDER_COOKIE = "__Test_Use_Mock_Provider";

async function setMockProviderCookie(page: Page): Promise<void> {
  await page.context().addCookies([
    {
      name: MOCK_PROVIDER_COOKIE,
      value: "true",
      url: "https://localhost:9000"
    }
  ]);
}

test.describe("@smoke", () => {
  /**
   * SUBSCRIPTION MANAGEMENT E2E TEST
   *
   * Tests the complete subscription lifecycle using MockStripeClient responses:
   * - Basis plan display with plan comparison cards (no-subscription view)
   * - Subscribe flow with billing info gate and mock checkout session callback
   * - Upgrade from Standard to Premium (plans page)
   * - Schedule downgrade from Premium to Standard (plans page with mocked state)
   * - Cancel subscription with reason selection (plans page)
   * - Cancelling state with reactivation banner and confirmation dialog
   * - Payment history table with invoice links
   * - PastDue warning banner display
   * - Suspension error page (Owner vs Member view)
   * - Stripe unconfigured state handling
   * - Access denied for non-Owner users
   */
  test("should handle complete subscription lifecycle with plan changes and billing states", async ({ ownerPage }) => {
    const context = createTestContext(ownerPage);

    // === BASIS STATE AND PLAN DISPLAY ===
    await step("Navigate to subscription page & verify Basis plan with comparison cards")(async () => {
      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByRole("heading", { name: "Subscription" })).toBeVisible();
      await expect(ownerPage.getByText("Choose a plan to get started.")).toBeVisible();

      const basisCard = ownerPage.locator(".grid > div").filter({ hasText: "Basis" }).first();
      await expect(basisCard.getByText("Free")).toBeVisible();
      await expect(basisCard.getByText("5 users")).toBeVisible();
      await expect(basisCard.getByText("10 GB storage")).toBeVisible();
      await expect(basisCard.getByText("Basic support")).toBeVisible();
      await expect(basisCard.getByRole("button", { name: "Current plan" })).toBeDisabled();

      const standardCard = ownerPage.locator(".grid > div").filter({ hasText: "Standard" }).first();
      await expect(standardCard.getByText("EUR 19/month")).toBeVisible();
      await expect(standardCard.getByText("10 users")).toBeVisible();
      await expect(standardCard.getByText("100 GB storage")).toBeVisible();
      await expect(standardCard.getByText("Email support")).toBeVisible();
      await expect(standardCard.getByText("Analytics")).toBeVisible();
      await expect(standardCard.getByRole("button", { name: "Subscribe" })).toBeVisible();

      const premiumCard = ownerPage.locator(".grid > div").filter({ hasText: "Premium" }).first();
      await expect(premiumCard.getByText("EUR 39/month")).toBeVisible();
      await expect(premiumCard.getByText("Unlimited users")).toBeVisible();
      await expect(premiumCard.getByText("1 TB storage")).toBeVisible();
      await expect(premiumCard.getByText("Priority support")).toBeVisible();
      await expect(premiumCard.getByText("Advanced analytics")).toBeVisible();
      await expect(premiumCard.getByText("SLA")).toBeVisible();
      await expect(premiumCard.getByRole("button", { name: "Subscribe" })).toBeVisible();
    })();

    // === SUBSCRIBE FLOW (MOCK CHECKOUT) ===
    await step("Click Subscribe on Standard plan & fill billing info & create Stripe customer")(async () => {
      await setMockProviderCookie(ownerPage);

      await ownerPage.route("**/api/account/subscriptions/checkout", (route) => route.abort());

      const standardCard = ownerPage.locator(".grid > div").filter({ hasText: "Standard" }).first();
      await standardCard.getByRole("button", { name: "Subscribe" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Add billing information" })).toBeVisible();
      await ownerPage.getByLabel("Name").fill("Test Organization");
      await ownerPage.getByLabel("Address line 1").fill("Vestergade 12");
      await ownerPage.getByLabel("Postal code").fill("1456");
      await ownerPage.getByLabel("City").fill("Copenhagen");
      await ownerPage.getByLabel("Country").click();
      await expect(ownerPage.getByRole("listbox")).toBeVisible();
      await ownerPage.getByRole("option", { name: "Denmark" }).scrollIntoViewIfNeeded();
      await ownerPage.getByRole("option", { name: "Denmark" }).click();
      await ownerPage.getByLabel("Email").fill("billing@example.com");

      const billingInfoResponse = ownerPage.waitForResponse("**/api/account/subscriptions/billing-info");
      await ownerPage.getByRole("button", { name: "Next" }).click();
      await billingInfoResponse;
      await expectToastMessage(context, "Billing information updated");

      await ownerPage.unroute("**/api/account/subscriptions/checkout");
    })();

    await step("Simulate checkout success & verify subscription activated with payment history")(async () => {
      await ownerPage.goto("/account/subscription?session_id=mock_session_id");

      await expectToastMessage(context, "Your subscription has been activated.");

      await expect(ownerPage.getByText("Active")).toBeVisible();
      await expect(ownerPage.getByText("Standard", { exact: true }).first()).toBeVisible();
      await expect(ownerPage.getByText("Next billing date:")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Change" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update" })).toBeEnabled();

      await expect(ownerPage.getByRole("columnheader", { name: "Date" })).toBeVisible();
      await expect(ownerPage.getByRole("columnheader", { name: "Amount" })).toBeVisible();
      await expect(ownerPage.getByRole("columnheader", { name: "Status" })).toBeVisible();
      await expect(ownerPage.getByText("Succeeded")).toBeVisible();
      await expect(ownerPage.getByRole("link", { name: "Invoice" })).toBeVisible();
    })();

    // === UPGRADE FLOW ===
    await step("Navigate to plans page & click Upgrade on Premium plan & confirm upgrade dialog")(async () => {
      await ownerPage.goto("/account/subscription/plans");

      const premiumCard = ownerPage.locator(".grid > div").filter({ hasText: "Premium" }).first();
      await premiumCard.getByRole("button", { name: "Upgrade" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Upgrade to Premium")).toBeVisible();
      await expect(ownerPage.getByText("Your plan will be upgraded to Premium immediately")).toBeVisible();
      await ownerPage.getByRole("button", { name: "Confirm upgrade" }).click();

      await expectToastMessage(context, "Your plan has been upgraded.");
    })();

    // === DOWNGRADE FLOW (MOCKED PREMIUM STATE) ===
    await step("Mock Premium subscription state & click Downgrade on Standard plan")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Premium",
            scheduledPlan: null,
            currentPeriodEnd: "2026-02-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            hasStripeSubscription: true
          }
        });
      });

      await ownerPage.goto("/account/subscription/plans");
      await expect(ownerPage.getByText("Premium", { exact: true }).first()).toBeVisible();

      const standardCard = ownerPage.locator(".grid > div").filter({ hasText: "Standard" }).first();
      await standardCard.getByRole("button", { name: "Downgrade" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Downgrade to Standard")).toBeVisible();
      await expect(ownerPage.getByText("Your plan will be downgraded to Standard")).toBeVisible();
    })();

    await step("Confirm downgrade & verify scheduled downgrade toast")(async () => {
      await ownerPage.route("**/api/account/subscriptions/schedule-downgrade", async (route) => {
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.getByRole("button", { name: "Confirm downgrade" }).click();

      await expectToastMessage(context, "Your downgrade has been scheduled.");
      await ownerPage.unroute("**/api/account/subscriptions/schedule-downgrade");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === CANCEL SUBSCRIPTION ===
    await step("Navigate to plans page & click Cancel subscription & verify confirmation dialog")(async () => {
      await ownerPage.goto("/account/subscription/plans");

      await ownerPage.getByRole("button", { name: "Cancel subscription" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("will be suspended")).toBeVisible();
    })();

    await step("Select cancellation reason & confirm & verify subscription cancelled toast")(async () => {
      await ownerPage.route("**/api/account/subscriptions/cancel", async (route) => {
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.getByRole("radio", { name: "No longer needed" }).click();

      const dialog = ownerPage.getByRole("alertdialog");
      await dialog.getByRole("button", { name: "Cancel subscription" }).click();

      await expectToastMessage(context, "Your subscription has been cancelled.");
      await ownerPage.unroute("**/api/account/subscriptions/cancel");
    })();

    // === CANCELLING STATE (MOCKED) ===
    await step("Mock cancelling subscription state & verify cancellation banner with reactivate button")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            currentPeriodEnd: "2026-02-24T00:00:00Z",
            cancelAtPeriodEnd: true,
            hasStripeSubscription: true
          }
        });
      });

      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByText("Cancelling")).toBeVisible();
      await expect(ownerPage.getByText("Access until")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Reactivate" })).toBeVisible();
    })();

    // === REACTIVATE SUBSCRIPTION ===
    await step("Click Reactivate in banner & confirm dialog & verify subscription reactivated toast")(async () => {
      await ownerPage.route("**/api/account/subscriptions/reactivate", async (route) => {
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.getByRole("button", { name: "Reactivate" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Reactivate subscription")).toBeVisible();
      await ownerPage.getByRole("alertdialog").getByRole("button", { name: "Reactivate" }).click();

      await expectToastMessage(context, "Your subscription has been reactivated.");
      await ownerPage.unroute("**/api/account/subscriptions/reactivate");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === PAST DUE BANNER (MOCKED TENANT STATE) ===
    await step("Mock tenant PastDue state & verify warning banner displayed")(async () => {
      await ownerPage.route("**/api/account/tenants/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: 1,
            createdAt: "2026-01-01T00:00:00Z",
            modifiedAt: null,
            name: "Test Organization",
            state: "PastDue",
            logoUrl: null
          }
        });
      });

      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByText("Payment failed. Your subscription will be suspended soon.")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update payment method" }).first()).toBeVisible();
    })();

    // === SUSPENDED STATE (MOCKED TENANT STATE) ===
    await step("Mock tenant Suspended state & verify blocked page for Owner")(async () => {
      await ownerPage.route("**/api/account/tenants/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: 1,
            createdAt: "2026-01-01T00:00:00Z",
            modifiedAt: null,
            name: "Test Organization",
            state: "Suspended",
            logoUrl: null
          }
        });
      });

      await ownerPage.goto("/account");
      await expect(ownerPage.getByRole("heading", { name: "Payment failed" })).toBeVisible();
      await expect(
        ownerPage.getByText(
          "Your subscription has been suspended due to a failed payment. Please update your payment method to restore access."
        )
      ).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update payment method" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Reactivate subscription" })).toBeVisible();
    })();

    await step("Navigate to subscription page while Suspended & verify access is allowed")(async () => {
      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByRole("heading", { name: "Subscription", exact: true })).toBeVisible();

      await ownerPage.unroute("**/api/account/tenants/current");
    })();

    // === STRIPE UNCONFIGURED STATE ===
    await step("Mock Stripe unconfigured state & verify warning message on plans page")(async () => {
      await ownerPage.route("**/api/account/subscriptions/stripe-health", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            isConfigured: false,
            hasApiKey: false,
            hasWebhookSecret: false,
            hasStandardPriceId: false,
            hasPremiumPriceId: false
          }
        });
      });

      await ownerPage.goto("/account/subscription/plans");
      await expect(
        ownerPage.getByText("Billing is not configured. Please contact support to enable payment processing.")
      ).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/stripe-health");
    })();
  });

  test("should deny subscription page access to non-Owner users", async ({ ownerPage }) => {
    createTestContext(ownerPage);

    await step("Mock Member role & navigate to subscription page & verify access denied")(async () => {
      await ownerPage.addInitScript(() => {
        let originalFn: (() => { userInfoEnv: { role: string } }) | null = null;
        Object.defineProperty(window, "getApplicationEnvironment", {
          configurable: true,
          set(fn: () => { userInfoEnv: { role: string } }) {
            originalFn = fn;
          },
          get() {
            if (!originalFn) {
              return undefined;
            }
            return () => {
              const env = originalFn?.();
              return { ...env, userInfoEnv: { ...env?.userInfoEnv, role: "Member" } };
            };
          }
        });
      });

      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByRole("heading", { name: "Access denied" })).toBeVisible();
      await expect(ownerPage.getByText("You do not have permission to access this page.")).toBeVisible();
    })();

    // === SUSPENDED STATE FOR NON-OWNER ===
    await step("Mock Suspended tenant with Member role & verify contact owner message")(async () => {
      await ownerPage.route("**/api/account/tenants/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: 1,
            createdAt: "2026-01-01T00:00:00Z",
            modifiedAt: null,
            name: "Test Organization",
            state: "Suspended",
            logoUrl: null
          }
        });
      });

      await ownerPage.goto("/account");
      await expect(ownerPage.getByRole("heading", { name: "Payment failed" })).toBeVisible();
      await expect(
        ownerPage.getByText(
          "Your subscription has been suspended due to a failed payment. Please contact the account owner to restore access."
        )
      ).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update payment method" })).not.toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Reactivate subscription" })).not.toBeVisible();

      await ownerPage.unroute("**/api/account/tenants/current");
    })();
  });
});
