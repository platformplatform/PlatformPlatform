import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  test("should handle language changes across signup, authentication, and logout flows", async ({ page }) => {
    const user = testUser();

    await step("Navigate to signup page & verify default English interface")(async () => {
      await page.goto("/signup");

      // Verify default English interface
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
    })();

    await step("Click language button and select Danish & verify interface updates")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const languageButton = page.getByRole("button", { name: "Change language" });
      await languageButton.dispatchEvent("click");

      const languageMenu = page.getByRole("menu");
      await expect(languageMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const danskMenuItem = page.getByRole("menuitem", { name: "Dansk" });
      await expect(danskMenuItem).toBeVisible();
      await danskMenuItem.dispatchEvent("click");

      // Verify interface updates to Danish and preference is saved
      await expect(page.getByRole("heading", { name: "Opret din konto" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Complete signup with Danish interface & verify language persists through flow")(async () => {
      await page.getByRole("textbox", { name: "E-mail" }).fill(user.email);
      await page.getByRole("button", { name: "Tilmeld dig med e-mail" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByRole("heading", { name: "Indtast din bekræftelseskode" })).toBeVisible();
    })();

    await step("Complete verification & complete welcome flow in Danish")(async () => {
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL(/\/welcome/);
      await page.getByRole("textbox", { name: "Kontonavn" }).fill("Test Organization");
      await page.getByRole("button", { name: "Fortsæt" }).click();

      await page.getByRole("textbox", { name: "Fornavn" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Efternavn" }).fill(user.lastName);
      await page.getByRole("button", { name: "Fortsæt" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    await step("Navigate to home & verify Danish interface")(async () => {
      await page.goto("/dashboard");
      await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    })();

    await step("Click logout from Danish interface & verify language persists after logout")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const triggerButton = page.getByRole("button", { name: "Brugermenu" });
      await triggerButton.dispatchEvent("click");

      const accountMenu = page.getByRole("menu", { name: "Brugermenu" });
      await expect(accountMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log ud" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      // Verify Danish language persists after logout
      await expect(page).toHaveURL("/login?returnPath=%2Fdashboard");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Change login page language to English & verify interface updates")(async () => {
      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const languageButton = page.getByRole("button", { name: "Skift sprog" });
      await languageButton.dispatchEvent("click");

      const languageMenu = page.getByRole("menu");
      await expect(languageMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const englishMenuItem = page.getByRole("menuitem", { name: "English" });
      await expect(englishMenuItem).toBeVisible();
      await englishMenuItem.dispatchEvent("click");

      // Verify interface updates to English
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();

    await step("Login with English interface & verify language resets to saved preference")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify?returnPath=%2Fdashboard");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
    })();

    await step("Complete login verification & verify language resets to user's saved preference")(async () => {
      await typeOneTimeCode(page, getVerificationCode());

      await expect(page).toHaveURL("/dashboard");
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Navigate to preferences & change language to English")(async () => {
      await page.goto("/user/preferences");
      await expect(page.getByRole("heading", { name: "Brugerindstillinger" })).toBeVisible();

      await page.getByRole("button", { name: "English" }).click();

      // Language change triggers page reload
      await expect(page.getByRole("heading", { name: "Preferences" })).toBeVisible();

      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();
  });

  test("should handle language persistence across different user sessions", async ({ browser }) => {
    const page1 = await (await browser.newContext()).newPage();
    const page2 = await (await browser.newContext()).newPage();

    createTestContext(page1);
    const testContext2 = createTestContext(page2);
    const user1 = testUser();
    const user2 = testUser();

    await step("Complete signup for first user with Danish & verify preference saved")(async () => {
      // Set up Danish interface
      await page1.goto("/signup");

      // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
      const languageButton = page1.getByRole("button", { name: "Change language" });
      await languageButton.dispatchEvent("click");

      const languageMenu = page1.getByRole("menu");
      await expect(languageMenu).toBeVisible();

      // Click menu item with JavaScript evaluate to bypass stability check during animation
      const danskMenuItem = page1.getByRole("menuitem", { name: "Dansk" });
      await expect(danskMenuItem).toBeVisible();
      await danskMenuItem.dispatchEvent("click");
      await expect(page1.getByRole("heading", { name: "Opret din konto" })).toBeVisible();

      // Complete signup flow
      await page1.getByRole("textbox", { name: "E-mail" }).fill(user1.email);
      await page1.getByRole("button", { name: "Tilmeld dig med e-mail" }).click();
      await expect(page1).toHaveURL("/signup/verify");

      await typeOneTimeCode(page1, getVerificationCode());
      await expect(page1).toHaveURL(/\/welcome/);

      // Complete welcome flow in Danish
      await page1.getByRole("textbox", { name: "Kontonavn" }).fill("Test Organization");
      await page1.getByRole("button", { name: "Fortsæt" }).click();

      await page1.getByRole("textbox", { name: "Fornavn" }).fill(user1.firstName);
      await page1.getByRole("textbox", { name: "Efternavn" }).fill(user1.lastName);
      await page1.getByRole("button", { name: "Fortsæt" }).click();

      await expect(page1).toHaveURL("/dashboard");
      await expect(page1.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(page1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Complete signup for second user with default English & verify different language preference")(
      async () => {
        await completeSignupFlow(page2, expect, user2, testContext2, true);

        await expect(page2.getByRole("heading", { level: 1 })).toBeVisible();
        await expect(page2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
      }
    )();

    await step("Login first user in new browser context & verify language preference persists")(async () => {
      const newContext1 = await browser.newContext();
      const newPage1 = await newContext1.newPage();

      // Login with English interface
      await newPage1.goto("/login");
      await newPage1.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await newPage1.getByRole("button", { name: "Log in with email" }).click();
      await expect(newPage1).toHaveURL("/login/verify");

      await typeOneTimeCode(newPage1, getVerificationCode());

      // Verify Danish preference is restored after login
      await expect(newPage1).toHaveURL("/dashboard");
      await expect(newPage1.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(newPage1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Login second user in new browser context & verify language preference persists")(async () => {
      const newContext2 = await browser.newContext();
      const newPage2 = await newContext2.newPage();

      // Login with English interface
      await newPage2.goto("/login");
      await newPage2.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await newPage2.getByRole("button", { name: "Log in with email" }).click();
      await expect(newPage2).toHaveURL("/login/verify");

      await typeOneTimeCode(newPage2, getVerificationCode());

      // Verify English preference is maintained
      await expect(newPage2).toHaveURL("/dashboard");
      await expect(newPage2.getByRole("heading", { level: 1 })).toBeVisible();
      await expect(newPage2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();
  });
});
