import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext, expectToastMessage } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@comprehensive", () => {
  test("should handle language changes across signup, authentication, and logout flows", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    await step("Navigate to signup page & verify default English interface")(async () => {
      await page.goto("/signup");

      // Verify default English interface
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();
    })();

    await step("Click language button and select Danish & verify interface updates")(async () => {
      await page.getByRole("button", { name: "Change language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();

      // Verify interface updates to Danish and preference is saved
      await expect(page.getByRole("heading", { name: "Opret din konto" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Complete signup with Danish interface & verify language persists through flow")(async () => {
      await page.getByRole("textbox", { name: "E-mail" }).fill(user.email);
      await page.getByRole("button", { name: "Opret din konto" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByRole("heading", { name: "Indtast din bekræftelseskode" })).toBeVisible();
    })();

    await step("Complete verification with Danish interface & verify navigation to admin")(async () => {
      // Auto-submits on 6 characters
      await page.keyboard.type(getVerificationCode());

      await expect(page).toHaveURL("/admin");
    })();

    await step("Complete profile setup in Danish & verify profile form works")(async () => {
      // Fill profile form in Danish
      await expect(page.getByRole("dialog", { name: "Brugerprofil" })).toBeVisible();
      await page.getByRole("textbox", { name: "Fornavn" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Efternavn" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Titel" }).fill("CEO");
      await page.getByRole("button", { name: "Gem ændringer" }).click();

      // Verify Danish success message and navigation
      await expectToastMessage(context, "Profil opdateret succesfuldt");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
    })();

    await step("Click logout from Danish interface & verify language persists after logout")(async () => {
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();

      // Verify Danish language persists after logout
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Change login page language to English & verify interface updates")(async () => {
      await page.getByRole("button", { name: "Skift sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();

      // Verify interface updates to English
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();

    await step("Login with English interface & verify language resets to saved preference")(async () => {
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();
    })();

    await step("Complete login verification & verify language resets to user's saved preference")(async () => {
      // Auto-submits on 6 characters
      await page.keyboard.type(getVerificationCode());

      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Click language button and reset to English & verify language change works")(async () => {
      await page.getByRole("button", { name: "Skift sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();

      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

      // Fix bug where localStorage is not updated before page reload
      await page.reload();

      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();
  });

  test("should handle language persistence across different user sessions", async ({ browser }) => {
    const page1 = await (await browser.newContext()).newPage();
    const page2 = await (await browser.newContext()).newPage();

    const testContext1 = createTestContext(page1);
    const testContext2 = createTestContext(page2);
    const user1 = testUser();
    const user2 = testUser();

    await step("Complete signup for first user with Danish & verify preference saved")(async () => {
      // Set up Danish interface
      await page1.goto("/signup");
      await page1.getByRole("button", { name: "Change language" }).click();
      await page1.getByRole("menuitem", { name: "Dansk" }).click();
      await expect(page1.getByRole("heading", { name: "Opret din konto" })).toBeVisible();

      // Complete signup flow
      await page1.getByRole("textbox", { name: "E-mail" }).fill(user1.email);
      await page1.getByRole("button", { name: "Opret din konto" }).click();
      await expect(page1).toHaveURL("/signup/verify");

      // Auto-submits on 6 characters
      await page1.keyboard.type(getVerificationCode());

      // Complete profile in Danish
      await page1.getByRole("textbox", { name: "Fornavn" }).fill(user1.firstName);
      await page1.getByRole("textbox", { name: "Efternavn" }).fill(user1.lastName);
      await page1.getByRole("button", { name: "Gem ændringer" }).click();

      // Verify Danish preference saved
      await expectToastMessage(testContext1, 200, "Profil opdateret succesfuldt");
      await expect(page1.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(page1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Complete signup for second user with default English & verify different language preference")(
      async () => {
        await completeSignupFlow(page2, expect, user2, testContext2, true);

        await expect(page2.getByRole("heading", { name: "Welcome home" })).toBeVisible();
        await expect(page2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
      }
    )();

    await step("Login first user in new browser context & verify language preference persists")(async () => {
      const newContext1 = await browser.newContext();
      const newPage1 = await newContext1.newPage();

      // Login with English interface
      await newPage1.goto("/login");
      await newPage1.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await newPage1.getByRole("button", { name: "Continue" }).click();
      await expect(newPage1).toHaveURL("/login/verify");

      // Auto-submits on 6 characters
      await newPage1.keyboard.type(getVerificationCode());

      // Verify Danish preference is restored after login
      await expect(newPage1).toHaveURL("/admin");
      await expect(newPage1.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(newPage1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");
    })();

    await step("Login second user in new browser context & verify language preference persists")(async () => {
      const newContext2 = await browser.newContext();
      const newPage2 = await newContext2.newPage();

      // Login with English interface
      await newPage2.goto("/login");
      await newPage2.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await newPage2.getByRole("button", { name: "Continue" }).click();
      await expect(newPage2).toHaveURL("/login/verify");

      // Auto-submits on 6 characters
      await newPage2.keyboard.type(getVerificationCode());

      // Verify English preference is maintained
      await expect(newPage2).toHaveURL("/admin");
      await expect(newPage2.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(newPage2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    })();
  });
});
