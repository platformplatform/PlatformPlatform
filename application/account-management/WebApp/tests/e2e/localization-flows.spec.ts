import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { step } from "@shared/e2e/utils/step-decorator";
import { assertToastMessage, createTestContext } from "@shared/e2e/utils/test-assertions";
import { completeSignupFlow, getVerificationCode, testUser } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("Localization Flow", () => {
  test.describe("@comprehensive", () => {
    test("should handle language changes across signup, authentication, and logout flows", async ({ page }) => {
      const context = createTestContext(page);
      const user = testUser();

      // Act & Assert: Navigate to signup page & verify default English interface
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      // Act & Assert: Change language to Danish on signup page & verify interface updates
      await page.getByRole("button", { name: "Select language" }).click();
      await page.getByRole("menuitem", { name: "Dansk" }).click();
      await expect(page.getByRole("heading", { name: "Opret din konto" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");

      // Act & Assert: Complete signup with Danish interface & verify language persists through flow
      await page.getByRole("textbox", { name: "E-mail" }).fill(user.email);
      await page.getByRole("button", { name: "Opret din konto" }).click();
      await expect(page).toHaveURL("/signup/verify");
      await expect(page.getByRole("heading", { name: "Indtast din bekræftelseskode" })).toBeVisible();

      // Act & Assert: Complete verification with Danish interface & verify navigation to admin
      await page.keyboard.type(getVerificationCode()); // The verification code auto submits
      await expect(page).toHaveURL("/admin");

      // Act & Assert: Complete profile setup in Danish & verify profile form works
      await expect(page.getByRole("dialog", { name: "Brugerprofil" })).toBeVisible();
      await page.getByRole("textbox", { name: "Fornavn" }).fill(user.firstName);
      await page.getByRole("textbox", { name: "Efternavn" }).fill(user.lastName);
      await page.getByRole("textbox", { name: "Titel" }).fill("CEO");
      await page.getByRole("button", { name: "Gem ændringer" }).click();
      await assertToastMessage(context, "Succes", "Profil opdateret succesfuldt");
      await expect(page.getByRole("dialog")).not.toBeVisible();
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();

      // Act & Assert: Logout from Danish interface & verify language persists after logout
      await page.getByRole("button", { name: "Brugerprofilmenu" }).click();
      await page.getByRole("menuitem", { name: "Log ud" }).click();
      await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Hej! Velkommen tilbage" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");

      // Act & Assert: Change login page language to English & verify interface updates
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Hi! Welcome back" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");

      // Act & Assert: Login with English interface & verify language resets to user's saved preference (Danish)
      await page.getByRole("textbox", { name: "Email" }).fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();
      await expect(page).toHaveURL("/login/verify?returnPath=%2Fadmin");
      await expect(page.getByRole("heading", { name: "Enter your verification code" })).toBeVisible();

      // Act & Assert: Complete login verification & verify language resets to user's saved preference
      await page.keyboard.type(getVerificationCode()); // The verification code auto submits
      await expect(page).toHaveURL("/admin");
      await expect(page.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");

      // Act & Assert: Reset to English for cleanup & verify language change works
      await page.getByRole("button", { name: "Vælg sprog" }).click();
      await page.getByRole("menuitem", { name: "English" }).click();
      await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await page.reload(); // Fix bug where localStorage is not updated before page reload
      await expect(page.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");
    });

    test("should handle language persistence across different user sessions", async ({ browser }) => {
      const context1 = await browser.newContext();
      const context2 = await browser.newContext();
      const page1 = await context1.newPage();
      const page2 = await context2.newPage();

      const testContext1 = createTestContext(page1);
      const testContext2 = createTestContext(page2);
      const user1 = testUser();
      const user2 = testUser();

      // Act & Assert: Complete signup for first user with Danish language & verify language preference saved
      await page1.goto("/signup");
      await page1.getByRole("button", { name: "Select language" }).click();
      await page1.getByRole("menuitem", { name: "Dansk" }).click();
      await expect(page1.getByRole("heading", { name: "Opret din konto" })).toBeVisible();
      await page1.getByRole("textbox", { name: "E-mail" }).fill(user1.email);
      await page1.getByRole("button", { name: "Opret din konto" }).click();
      await expect(page1).toHaveURL("/signup/verify");
      await page1.keyboard.type(getVerificationCode()); // The verification code auto submits
      await page1.getByRole("textbox", { name: "Fornavn" }).fill(user1.firstName);
      await page1.getByRole("textbox", { name: "Efternavn" }).fill(user1.lastName);
      await page1.getByRole("button", { name: "Gem ændringer" }).click();
      await assertToastMessage(testContext1, "Succes", "Profil opdateret succesfuldt");
      await expect(page1.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(page1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");

      // Act & Assert: Complete signup for second user with default English & verify different language preference
      await completeSignupFlow(page2, expect, user2, testContext2, true);
      await expect(page2.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(page2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");

      // Act & Assert: Login first user in new session & verify Danish language is restored from database
      const newContext1 = await browser.newContext();
      const newPage1 = await newContext1.newPage();

      await newPage1.goto("/login");
      await newPage1.getByRole("textbox", { name: "Email" }).fill(user1.email);
      await newPage1.getByRole("button", { name: "Continue" }).click();
      await expect(newPage1).toHaveURL("/login/verify");
      await newPage1.keyboard.type(getVerificationCode()); // The verification code auto submits
      await expect(newPage1).toHaveURL("/admin");
      await expect(newPage1.getByRole("heading", { name: "Velkommen hjem" })).toBeVisible();
      await expect(newPage1.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("da-DK");

      // Act & Assert: Login second user in new session & verify English language is restored from database
      const newContext2 = await browser.newContext();
      const newPage2 = await newContext2.newPage();

      await newPage2.goto("/login");
      await newPage2.getByRole("textbox", { name: "Email" }).fill(user2.email);
      await newPage2.getByRole("button", { name: "Continue" }).click();
      await expect(newPage2).toHaveURL("/login/verify");
      await newPage2.keyboard.type(getVerificationCode()); // The verification code auto submits
      await expect(newPage2).toHaveURL("/admin");
      await expect(newPage2.getByRole("heading", { name: "Welcome home" })).toBeVisible();
      await expect(newPage2.evaluate(() => localStorage.getItem("preferred-locale"))).resolves.toBe("en-US");

      // Act & Assert: Cleanup browser contexts & verify no errors
      await context1.close();
      await context2.close();
      await newContext1.close();
      await newContext2.close();
    });
  });
});
