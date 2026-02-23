import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

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
   * - Payment failed warning banner display
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
      await expect(standardCard.getByText("/month")).toBeVisible();
      await expect(standardCard.getByText("10 users")).toBeVisible();
      await expect(standardCard.getByText("100 GB storage")).toBeVisible();
      await expect(standardCard.getByText("Email support")).toBeVisible();
      await expect(standardCard.getByText("Analytics")).toBeVisible();
      await expect(standardCard.getByRole("button", { name: "Subscribe" })).toBeVisible();

      const premiumCard = ownerPage.locator(".grid > div").filter({ hasText: "Premium" }).first();
      await expect(premiumCard.getByText("/month")).toBeVisible();
      await expect(premiumCard.getByText("Unlimited users")).toBeVisible();
      await expect(premiumCard.getByText("1 TB storage")).toBeVisible();
      await expect(premiumCard.getByText("Priority support")).toBeVisible();
      await expect(premiumCard.getByText("Advanced analytics")).toBeVisible();
      await expect(premiumCard.getByText("SLA")).toBeVisible();
      await expect(premiumCard.getByRole("button", { name: "Subscribe" })).toBeVisible();
    })();

    // === SUBSCRIBE FLOW (BILLING INFO DIALOG + MOCKED ACTIVE STATE) ===
    await step("Click Subscribe on Standard plan & verify billing info dialog fields")(async () => {
      const standardCard = ownerPage.locator(".grid > div").filter({ hasText: "Standard" }).first();
      await standardCard.getByRole("button", { name: "Subscribe" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Add billing information" })).toBeVisible();
      await expect(ownerPage.getByLabel("Billing email")).toBeVisible();
      await expect(ownerPage.getByLabel("Country")).toBeVisible();
      await expect(ownerPage.getByLabel("Name")).toBeVisible();
      await expect(ownerPage.getByLabel("Address")).toBeVisible();
      await expect(ownerPage.getByLabel("Postal code")).toBeVisible();
      await expect(ownerPage.getByLabel("City")).toBeVisible();
      await expect(ownerPage.getByLabel("Tax ID (VAT number)")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Next" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Cancel" })).toBeVisible();

      await ownerPage.getByRole("button", { name: "Cancel" }).click();
      await expect(ownerPage.getByRole("heading", { name: "Add billing information" })).not.toBeVisible();
    })();

    await step("Mock active Standard subscription & verify subscription overview with payment history")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: { brand: "visa", last4: "4242", expMonth: 12, expYear: 2026 },
            billingInfo: {
              name: "Test Organization",
              address: {
                line1: "Vestergade 12",
                line2: null,
                postalCode: "1456",
                city: "Copenhagen",
                state: null,
                country: "DK"
              },
              email: "billing@example.com",
              taxId: null
            },
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/payment-history**", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            totalCount: 1,
            transactions: [
              {
                id: "txn_mock_1",
                amount: 29.0,
                currency: "USD",
                status: "Succeeded",
                date: "2026-02-24T00:00:00Z",
                invoiceUrl: "https://mock.stripe.local/invoice/12345",
                creditNoteUrl: null
              }
            ]
          }
        });
      });

      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByText("Standard", { exact: false }).first()).toBeVisible();
      await expect(ownerPage.getByText("Active")).toBeVisible();
      await expect(ownerPage.getByText("Next billing date:")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Change plan" })).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update payment method" })).toBeEnabled();

      await expect(ownerPage.getByRole("heading", { name: "Payment method" })).toBeVisible();
      await expect(ownerPage.getByText("4242")).toBeVisible();
      await expect(ownerPage.getByRole("heading", { name: "Billing information" })).toBeVisible();
      await expect(ownerPage.getByText("billing@example.com")).toBeVisible();

      await expect(ownerPage.getByRole("columnheader", { name: "Date" })).toBeVisible();
      await expect(ownerPage.getByRole("columnheader", { name: "Amount" })).toBeVisible();
      await expect(ownerPage.getByRole("columnheader", { name: "Status" })).toBeVisible();
      await expect(ownerPage.getByText("Succeeded")).toBeVisible();
      await expect(ownerPage.getByRole("link", { name: "Invoice" })).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/current");
      await ownerPage.unroute("**/api/account/subscriptions/payment-history**");
    })();

    // === UPGRADE FLOW ===
    await step("Mock Standard subscription & click Upgrade on Premium plan & confirm upgrade dialog")(async () => {
      let currentPlan = "Standard";
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: currentPlan,
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: { brand: "visa", last4: "4242", expMonth: 12, expYear: 2026 },
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/upgrade-preview**", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            lineItems: [
              { description: "Premium (prorated)", amount: 70.0, currency: "USD", isTax: false },
              { description: "Tax", amount: 0, currency: "USD", isTax: true }
            ],
            totalAmount: 70.0,
            currency: "USD"
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/upgrade", async (route) => {
        currentPlan = "Premium";
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: { clientSecret: null, publishableKey: null }
        });
      });

      await ownerPage.goto("/account/subscription/plans");

      const premiumCard = ownerPage.locator(".grid > div").filter({ hasText: "Premium" }).first();
      await premiumCard.getByRole("button", { name: "Upgrade" }).click();

      await expect(ownerPage.getByRole("dialog", { name: "Upgrade to Premium" })).toBeVisible();
      await expect(ownerPage.getByText("Bill to")).toBeVisible();
      await expect(ownerPage.getByText("Payment method")).toBeVisible();
      await ownerPage.getByRole("button", { name: "Pay and upgrade" }).click();

      await expectToastMessage(context, "Your plan has been upgraded.");
      await ownerPage.unroute("**/api/account/subscriptions/upgrade-preview**");
      await ownerPage.unroute("**/api/account/subscriptions/upgrade");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === DOWNGRADE FLOW (MOCKED PREMIUM STATE) ===
    await step("Mock Premium subscription state & click Downgrade on Standard plan")(async () => {
      let scheduledPlan: string | null = null;
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Premium",
            scheduledPlan,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 99.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/schedule-downgrade", async (route) => {
        scheduledPlan = "Standard";
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.goto("/account/subscription/plans");

      const premiumCard = ownerPage.locator(".grid > div").filter({ hasText: "Premium" }).first();
      await expect(premiumCard.getByRole("button", { name: "Current plan" })).toBeDisabled();

      const standardCard = ownerPage.locator(".grid > div").filter({ hasText: "Standard" }).first();
      await standardCard.getByRole("button", { name: "Downgrade" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Downgrade to Standard")).toBeVisible();
      await expect(ownerPage.getByText("Your plan will be downgraded to Standard")).toBeVisible();
    })();

    await step("Confirm downgrade & verify scheduled downgrade toast")(async () => {
      await ownerPage.getByRole("button", { name: "Confirm downgrade" }).click();

      await expectToastMessage(context, "Your downgrade has been scheduled.");
      await ownerPage.unroute("**/api/account/subscriptions/schedule-downgrade");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === CANCEL SUBSCRIPTION ===
    await step("Mock Standard subscription & click Cancel subscription & verify confirmation dialog")(async () => {
      let cancelAtPeriodEnd = false;
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd,
            isPaymentFailed: false,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/cancel", async (route) => {
        cancelAtPeriodEnd = true;
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.goto("/account/subscription/plans");

      await ownerPage.getByRole("button", { name: "Cancel subscription" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("will switch to the free plan")).toBeVisible();
    })();

    await step("Select cancellation reason & confirm & verify subscription cancelled toast")(async () => {
      await ownerPage.getByRole("radio", { name: "No longer needed" }).click();

      const dialog = ownerPage.getByRole("alertdialog");
      await dialog.getByRole("button", { name: "Cancel subscription" }).click();

      await expectToastMessage(context, "Your subscription has been cancelled.");
      await ownerPage.unroute("**/api/account/subscriptions/cancel");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === CANCELLING STATE (MOCKED) ===
    await step("Mock cancelling subscription state & verify cancellation banner with reactivate button")(async () => {
      let cancelAtPeriodEnd = true;
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd,
            isPaymentFailed: false,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByText("Cancelling")).toBeVisible();
      await expect(ownerPage.getByText("Access until")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Reactivate" })).toBeVisible();

      await ownerPage.route("**/api/account/subscriptions/reactivate", async (route) => {
        cancelAtPeriodEnd = false;
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: { clientSecret: null, publishableKey: null }
        });
      });
    })();

    // === REACTIVATE SUBSCRIPTION ===
    await step("Click Reactivate in banner & confirm dialog & verify subscription reactivated toast")(async () => {
      await ownerPage.getByRole("button", { name: "Reactivate" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Reactivate subscription")).toBeVisible();
      await ownerPage.getByRole("alertdialog").getByRole("button", { name: "Reactivate" }).click();

      await expectToastMessage(context, "Your subscription has been reactivated.");
      await ownerPage.unroute("**/api/account/subscriptions/reactivate");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === PAYMENT FAILED BANNER (MOCKED SUBSCRIPTION STATE) ===
    await step("Mock subscription payment failed & verify warning banner displayed")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: true,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByText("Payment failed. Your subscription will be suspended soon.")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Update payment method" }).first()).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/current");
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
      await expect(ownerPage.getByRole("heading", { name: "Account suspended" })).toBeVisible();
      await expect(
        ownerPage.getByText(
          "Your account has been suspended. Please visit the subscription page to resolve any issues and restore access."
        )
      ).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Manage subscription" })).toBeVisible();
    })();

    await step("Navigate to subscription page while Suspended & verify access is allowed")(async () => {
      await ownerPage.goto("/account/subscription");
      await expect(ownerPage.getByRole("heading", { name: "Subscription", exact: true })).toBeVisible();

      await ownerPage.unroute("**/api/account/tenants/current");
    })();

    // === STRIPE UNCONFIGURED STATE ===
    await step("Mock empty pricing catalog & verify warning message on plans page")(async () => {
      await ownerPage.route("**/api/account/subscriptions/pricing-catalog", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            plans: []
          }
        });
      });

      await ownerPage.goto("/account/subscription/plans");
      await expect(
        ownerPage.getByText("Billing is not configured. Please contact support to enable payment processing.")
      ).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/pricing-catalog");
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
      await expect(ownerPage.getByRole("heading", { name: "Account suspended" })).toBeVisible();
      await expect(
        ownerPage.getByText("Your account has been suspended. Please contact the account owner to restore access.")
      ).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Manage subscription" })).not.toBeVisible();

      await ownerPage.unroute("**/api/account/tenants/current");
    })();
  });
});

test.describe("@comprehensive", () => {
  /**
   * SUBSCRIPTION EDGE CASES AND BILLING DETAILS E2E TEST
   *
   * Tests subscription features not covered by the smoke test:
   * - Tab navigation between Overview and Plans pages
   * - Scheduled downgrade banner on overview page with Cancel downgrade dialog
   * - Edit billing info dialog with form fields and Tax ID display
   * - Payment history with refunded transaction and credit note link
   * - Empty payment history state
   * - Billing info display with full address details
   */
  test("should handle tab navigation, scheduled downgrade banner, billing info editing, and payment history edge cases", async ({
    ownerPage
  }) => {
    const context = createTestContext(ownerPage);

    // === TAB NAVIGATION ===
    await step("Mock active subscription & navigate to overview & verify tab navigation to Plans")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: { brand: "visa", last4: "4242", expMonth: 12, expYear: 2026 },
            billingInfo: {
              name: "Test Organization",
              address: {
                line1: "Vestergade 12",
                line2: null,
                postalCode: "1456",
                city: "Copenhagen",
                state: null,
                country: "DK"
              },
              email: "billing@example.com",
              taxId: "DK12345678"
            },
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/payment-history**", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: { totalCount: 0, transactions: [] }
        });
      });

      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByRole("tablist", { name: "Subscription tabs" })).toBeVisible();
      await ownerPage.getByRole("tab", { name: "Plans" }).click();

      await expect(ownerPage).toHaveURL("/account/subscription/plans");
    })();

    await step("Navigate back to Overview tab & verify overview content loads")(async () => {
      await ownerPage.getByRole("tab", { name: "Overview" }).click();

      await expect(ownerPage).toHaveURL("/account/subscription");
      await expect(ownerPage.getByRole("heading", { name: "Current plan" })).toBeVisible();
    })();

    // === BILLING INFO WITH TAX ID ===
    await step("Open edit billing info dialog & verify Tax ID and address fields pre-populated")(async () => {
      await expect(ownerPage.getByText("DK12345678")).toBeVisible();
      await expect(ownerPage.getByText("billing@example.com")).toBeVisible();
      await expect(ownerPage.getByText("Vestergade 12")).toBeVisible();

      await ownerPage.getByRole("button", { name: "Edit billing information" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Edit billing information" })).toBeVisible();
      await expect(ownerPage.getByLabel("Billing email")).toHaveValue("billing@example.com");
      await expect(ownerPage.getByLabel("Name")).toHaveValue("Test Organization");
      await expect(ownerPage.getByLabel("Tax ID (VAT number)")).toHaveValue("DK12345678");
      await expect(ownerPage.getByLabel("Postal code")).toHaveValue("1456");
      await expect(ownerPage.getByLabel("City")).toHaveValue("Copenhagen");

      await ownerPage.getByRole("button", { name: "Cancel" }).click();
      await expect(ownerPage.getByRole("heading", { name: "Edit billing information" })).not.toBeVisible();
    })();

    // === EMPTY PAYMENT HISTORY ===
    await step("Scroll to billing history & verify empty state message")(async () => {
      await expect(ownerPage.getByRole("heading", { name: "Billing history" })).toBeVisible();
      await expect(ownerPage.getByText("No payment history available.")).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/payment-history**");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === PAYMENT HISTORY WITH REFUNDED TRANSACTION AND CREDIT NOTE ===
    await step("Mock payment history with refunded transaction & verify credit note link")(async () => {
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Standard",
            scheduledPlan: null,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 29.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/payment-history**", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            totalCount: 2,
            transactions: [
              {
                id: "txn_mock_1",
                amount: 29.0,
                currency: "USD",
                status: "Succeeded",
                date: "2026-02-24T00:00:00Z",
                invoiceUrl: "https://mock.stripe.local/invoice/12345",
                creditNoteUrl: null
              },
              {
                id: "txn_mock_2",
                amount: 29.0,
                currency: "USD",
                status: "Refunded",
                date: "2026-01-24T00:00:00Z",
                invoiceUrl: "https://mock.stripe.local/invoice/12346",
                creditNoteUrl: "https://mock.stripe.local/credit-note/67890"
              }
            ]
          }
        });
      });

      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByText("Succeeded")).toBeVisible();
      await expect(ownerPage.getByText("Refunded")).toBeVisible();

      const invoiceLinks = ownerPage.getByRole("link", { name: "Invoice" });
      await expect(invoiceLinks.first()).toBeVisible();
      await expect(ownerPage.getByRole("link", { name: "Credit note" })).toBeVisible();

      await ownerPage.unroute("**/api/account/subscriptions/payment-history**");
      await ownerPage.unroute("**/api/account/subscriptions/current");
    })();

    // === SCHEDULED DOWNGRADE BANNER ON OVERVIEW ===
    await step("Mock scheduled downgrade state & verify downgrade banner with cancel button on overview")(async () => {
      let scheduledPlan: string | null = "Standard";
      await ownerPage.route("**/api/account/subscriptions/current", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: {
            id: "sub_mock",
            plan: "Premium",
            scheduledPlan,
            hasStripeCustomer: true,
            hasStripeSubscription: true,
            currentPriceAmount: 99.0,
            currentPriceCurrency: "USD",
            currentPeriodEnd: "2026-03-24T00:00:00Z",
            cancelAtPeriodEnd: false,
            isPaymentFailed: false,
            paymentMethod: null,
            billingInfo: null,
            hasPendingStripeEvents: false
          }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/payment-history**", async (route) => {
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          json: { totalCount: 0, transactions: [] }
        });
      });

      await ownerPage.route("**/api/account/subscriptions/cancel-downgrade", async (route) => {
        scheduledPlan = null;
        await route.fulfill({ status: 200, contentType: "application/json", json: {} });
      });

      await ownerPage.goto("/account/subscription");

      await expect(ownerPage.getByText("will be downgraded to Standard")).toBeVisible();
      await expect(ownerPage.getByRole("button", { name: "Cancel downgrade" })).toBeVisible();
    })();

    await step("Click Cancel downgrade & confirm dialog & verify downgrade cancelled toast")(async () => {
      await ownerPage.getByRole("button", { name: "Cancel downgrade" }).click();

      await expect(ownerPage.getByRole("alertdialog")).toBeVisible();
      await expect(ownerPage.getByText("Cancel scheduled downgrade")).toBeVisible();
      await ownerPage.getByRole("alertdialog").getByRole("button", { name: "Cancel downgrade" }).click();

      await expectToastMessage(context, "Your scheduled downgrade has been cancelled.");
      await ownerPage.unroute("**/api/account/subscriptions/cancel-downgrade");
      await ownerPage.unroute("**/api/account/subscriptions/current");
      await ownerPage.unroute("**/api/account/subscriptions/payment-history**");
    })();
  });
});
