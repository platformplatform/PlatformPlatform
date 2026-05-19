import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { productName } from "@shared/e2e/utils/constants";
import { createTestContext, expectToastMessage, typeOneTimeCode } from "@shared/e2e/utils/test-assertions";
import { getVerificationCode, testUser, uniqueEmail } from "@shared/e2e/utils/test-data";
import { step } from "@shared/e2e/utils/test-step-wrapper";
import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";

function getMailpitBaseUrl(): string {
  const repoRoot = resolve(__dirname, "..", "..", "..", "..", "..");
  const portFile = join(repoRoot, ".workspace", "port.txt");
  if (!existsSync(portFile)) return "http://localhost:9005";
  const basePort = Number.parseInt(readFileSync(portFile, "utf8").trim(), 10);
  return `http://localhost:${basePort + 5}`;
}

const MAILPIT_BASE = getMailpitBaseUrl();

interface MailpitMessage {
  subject: string;
  html: string;
  text: string;
}

async function fetchLatestMailByRecipient(recipient: string): Promise<MailpitMessage> {
  const searchResponse = await fetch(`${MAILPIT_BASE}/api/v1/search?query=to:${encodeURIComponent(recipient)}&limit=1`);
  const searchData = (await searchResponse.json()) as { messages: { ID: string }[] };
  const messageId = searchData.messages[0]?.ID;
  if (!messageId) {
    throw new Error(`No email found in Mailpit for recipient: ${recipient}`);
  }
  const messageResponse = await fetch(`${MAILPIT_BASE}/api/v1/message/${messageId}`);
  const message = (await messageResponse.json()) as { Subject: string; HTML: string; Text: string };
  return { subject: message.Subject, html: message.HTML, text: message.Text };
}

test.describe("@smoke", () => {
  /**
   * Localized transactional email content tests for signup and login OTP flows.
   *
   * Covers:
   * - Signup OTP email in en-US and da-DK (subject, HTML, plaintext, iOS autofill suffix)
   * - Login OTP email in en-US (subject, HTML, plaintext)
   * - autocomplete="one-time-code" on signup and login verify pages
   */
  test("should send localized signup and login OTP emails and assert autocomplete attribute", async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();
    createTestContext(page);

    // === EN-US SIGNUP OTP ===
    const enSignupUser = testUser();

    await step("Sign up en-US user & verify OTP input with autocomplete=one-time-code on verify page")(async () => {
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

      await page.getByRole("textbox", { name: "Email" }).fill(enSignupUser.email);
      await page.getByRole("button", { name: "Sign up with email" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeVisible();
    })();

    await step("Fetch en-US signup OTP email from Mailpit & assert subject, HTML, plaintext and iOS autofill suffix")(
      async () => {
        const mail = await fetchLatestMailByRecipient(enSignupUser.email);

        expect(mail.subject).toBe("Confirm your email address");
        expect(mail.html).toContain("Your confirmation code is below");
        expect(mail.html).toContain("Enter it in your open browser window. It is only valid for a few minutes.");
        expect(mail.text).toContain("Your confirmation code is below");
        expect(mail.text).toContain("Enter it in your open browser window. It is only valid for a few minutes.");
        expect(mail.text).toContain("@app.dev.localhost #");
      }
    )();

    await step("Complete en-US signup & reach dashboard")(async () => {
      await typeOneTimeCode(page, getVerificationCode());
      await expect(page).toHaveURL(/\/welcome/);

      await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
      await page.getByRole("button", { name: "Continue" }).click();
      await page.getByRole("textbox", { name: "First name" }).fill(enSignupUser.firstName);
      await page.getByRole("textbox", { name: "Last name" }).fill(enSignupUser.lastName);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    await step("Log out en-US user")(async () => {
      const triggerButton = page.getByRole("button", { name: "User menu" });
      await triggerButton.dispatchEvent("click");
      const accountMenu = page.getByRole("menu");
      await expect(accountMenu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL(/\/login/);
    })();

    await step("Initiate en-US login & assert autocomplete=one-time-code on login verify page")(async () => {
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(enSignupUser.email);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeVisible();
    })();

    await step("Fetch en-US login OTP email from Mailpit & assert subject, HTML, plaintext and iOS autofill suffix")(
      async () => {
        const mail = await fetchLatestMailByRecipient(enSignupUser.email);

        expect(mail.subject).toBe(`${productName} login verification code`);
        expect(mail.html).toContain("Your confirmation code is below");
        expect(mail.html).toContain("Enter it in your open browser window. It is only valid for a few minutes.");
        expect(mail.text).toContain("Your confirmation code is below");
        expect(mail.text).toContain("Enter it in your open browser window. It is only valid for a few minutes.");
        expect(mail.text).toContain("@app.dev.localhost #");
      }
    )();

    // === DA-DK SIGNUP OTP ===
    const daSignupUser = testUser();

    await step("Sign up da-DK user & verify OTP input with autocomplete=one-time-code on verify page")(async () => {
      await page.evaluate(() => localStorage.setItem("preferred-locale", "da-DK"));
      await page.goto("/signup");
      await expect(page.getByRole("heading", { name: "Opret din konto" })).toBeVisible();

      await page.getByRole("textbox", { name: "E-mail" }).fill(daSignupUser.email);
      await page.getByRole("button", { name: "Tilmeld dig med e-mail" }).click();

      await expect(page).toHaveURL("/signup/verify");
      await expect(page.locator('input[autocomplete="one-time-code"]').first()).toBeVisible();
    })();

    await step(
      "Fetch da-DK signup OTP email from Mailpit & assert localized subject, HTML, plaintext and iOS autofill suffix"
    )(async () => {
      const mail = await fetchLatestMailByRecipient(daSignupUser.email);

      expect(mail.subject).toBe("Bekræft din e-mailadresse");
      expect(mail.html).toContain("Din bekræftelseskode står herunder");
      expect(mail.html).toContain("Indtast den i dit åbne browservindue. Den er kun gyldig i få minutter.");
      expect(mail.text).toContain("Din bekræftelseskode står herunder");
      expect(mail.text).toContain("Indtast den i dit åbne browservindue. Den er kun gyldig i få minutter.");
      expect(mail.text).toContain("@app.dev.localhost #");
    })();

    await context.close();
  });
});

test.describe("@comprehensive", () => {
  /**
   * Localized transactional email content tests for da-DK login OTP, unknown user, and invite flows.
   *
   * Covers:
   * - da-DK login OTP email (subject, HTML, plaintext, iOS autofill suffix)
   * - en-US and da-DK unknown user notification emails (subject, HTML, plaintext)
   * - en-US and da-DK invite user emails (subject, HTML, plaintext)
   */
  test("should send localized transactional emails for login, unknown user, and invite flows", async ({ browser }) => {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    createTestContext(page);
    const owner = testUser();

    // === DA-DK LOGIN OTP ===
    await step("Complete da-DK signup & reach dashboard")(async () => {
      await page.goto("/signup");
      await page.evaluate(() => localStorage.setItem("preferred-locale", "da-DK"));
      await page.reload();
      await expect(page.getByRole("heading", { name: "Opret din konto" })).toBeVisible();

      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Tilmeld dig med e-mail" }).click();
      await expect(page).toHaveURL("/signup/verify");

      await typeOneTimeCode(page, getVerificationCode());
      await expect(page).toHaveURL(/\/welcome/);

      await page.getByRole("textbox", { name: "Kontonavn" }).fill("Test Organisation");
      await page.getByRole("button", { name: "Fortsæt" }).click();
      await page.getByRole("textbox", { name: "Fornavn" }).fill(owner.firstName);
      await page.getByRole("textbox", { name: "Efternavn" }).fill(owner.lastName);
      await page.getByRole("button", { name: "Fortsæt" }).click();

      await expect(page).toHaveURL("/dashboard");
    })();

    await step("Log out da-DK user & initiate login to trigger da-DK login OTP email")(async () => {
      const triggerButton = page.getByRole("button", { name: "Brugermenu" });
      await triggerButton.dispatchEvent("click");
      const accountMenu = page.getByRole("menu", { name: "Brugermenu" });
      await expect(accountMenu).toBeVisible();
      const logoutMenuItem = page.getByRole("menuitem", { name: "Log ud" });
      await expect(logoutMenuItem).toBeVisible();
      await logoutMenuItem.dispatchEvent("click");

      await expect(page).toHaveURL(/\/login/);

      await page.goto("/login");
      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Log ind med e-mail" }).click();

      await expect(page).toHaveURL("/login/verify");
    })();

    await step(
      "Fetch da-DK login OTP email from Mailpit & assert localized subject, HTML, plaintext and iOS autofill suffix"
    )(async () => {
      const mail = await fetchLatestMailByRecipient(owner.email);

      expect(mail.subject).toBe(`${productName}-bekræftelseskode til login`);
      expect(mail.html).toContain("Din bekræftelseskode står herunder");
      expect(mail.html).toContain("Indtast den i dit åbne browservindue. Den er kun gyldig i få minutter.");
      expect(mail.text).toContain("Din bekræftelseskode står herunder");
      expect(mail.text).toContain("Indtast den i dit åbne browservindue. Den er kun gyldig i få minutter.");
      expect(mail.text).toContain("@app.dev.localhost #");
    })();

    // === EN-US UNKNOWN USER ===
    const enUnknownEmail = uniqueEmail();

    await step("Trigger en-US unknown user email by logging in with unknown address")(async () => {
      await page.evaluate(() => localStorage.setItem("preferred-locale", "en-US"));
      await page.goto("/login");
      await page.getByRole("textbox", { name: "Email" }).fill(enUnknownEmail);
      await page.getByRole("button", { name: "Log in with email" }).click();

      await expect(page).toHaveURL("/login/verify");
    })();

    await step("Fetch en-US unknown user email from Mailpit & assert subject, HTML, and plaintext")(async () => {
      const mail = await fetchLatestMailByRecipient(enUnknownEmail);

      expect(mail.subject).toBe("No account found");
      expect(mail.html).toContain("Is this the right email address?");
      expect(mail.html).toContain(enUnknownEmail);
      expect(mail.text).toContain("Is this the right email address?");
      expect(mail.text).toContain(enUnknownEmail);
    })();

    // === DA-DK UNKNOWN USER ===
    const daUnknownEmail = uniqueEmail();

    await step("Trigger da-DK unknown user email by logging in with unknown address")(async () => {
      await page.evaluate(() => localStorage.setItem("preferred-locale", "da-DK"));
      await page.goto("/login");
      await page.getByRole("textbox", { name: "E-mail" }).fill(daUnknownEmail);
      await page.getByRole("button", { name: "Log ind med e-mail" }).click();

      await expect(page).toHaveURL("/login/verify");
    })();

    await step("Fetch da-DK unknown user email from Mailpit & assert localized subject, HTML, and plaintext")(
      async () => {
        const mail = await fetchLatestMailByRecipient(daUnknownEmail);

        expect(mail.subject).toBe("Ingen konto fundet");
        expect(mail.html).toContain("Er det den rigtige e-mailadresse?");
        expect(mail.html).toContain(daUnknownEmail);
        expect(mail.text).toContain("Er det den rigtige e-mailadresse?");
        expect(mail.text).toContain(daUnknownEmail);
      }
    )();

    // === EN-US INVITE ===
    const enInviteeEmail = uniqueEmail();

    await step("Login as owner (da-DK) & switch locale to en-US via preferences & navigate to users page")(async () => {
      await page.goto("/login");
      await page.getByRole("textbox", { name: "E-mail" }).fill(owner.email);
      await page.getByRole("button", { name: "Log ind med e-mail" }).click();
      await expect(page).toHaveURL("/login/verify");

      await typeOneTimeCode(page, getVerificationCode());
      await expect(page).toHaveURL("/dashboard");

      await page.goto("/user/preferences");
      await expect(page.getByRole("heading", { name: "Brugerpræferencer" })).toBeVisible();
      await page.getByText("English").click();
      await expect(page.getByRole("heading", { name: "User preferences" })).toBeVisible();

      await page.goto("/account/users");
      await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    })();

    await step("Invite en-US user & assert en-US invite email subject, HTML, and plaintext")(async () => {
      await page.getByRole("button", { name: "Invite user" }).first().click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).toBeVisible();
      await page.getByRole("textbox", { name: "Email" }).fill(enInviteeEmail);
      await page.getByRole("button", { name: "Send invite" }).click();
      await expect(page.getByRole("dialog", { name: "Invite user" })).not.toBeVisible();

      const mail = await fetchLatestMailByRecipient(enInviteeEmail);

      expect(mail.subject).toContain("You have been invited to join");
      expect(mail.subject).toContain(productName);
      expect(mail.html).toContain(`invited you to join ${productName}`);
      expect(mail.html).toContain(enInviteeEmail);
      expect(mail.text).toContain(`invited you to join ${productName}`);
      expect(mail.text).toContain(enInviteeEmail);
    })();

    // === DA-DK INVITE ===
    const daInviteeEmail = uniqueEmail();

    await step("Switch locale to da-DK via preferences & invite da-DK user & assert localized invite email")(
      async () => {
        await page.goto("/user/preferences");
        await expect(page.getByRole("heading", { name: "User preferences" })).toBeVisible();
        await page.getByText("Dansk").click();
        await expect(page.getByRole("heading", { name: "Brugerpræferencer" })).toBeVisible();

        await page.goto("/account/users");
        await expect(page.getByRole("heading", { name: "Brugere" })).toBeVisible();

        await page.getByRole("button", { name: "Inviter bruger" }).first().click();
        await expect(page.getByRole("dialog", { name: "Inviter bruger" })).toBeVisible();
        await page.getByRole("textbox", { name: "E-mail" }).fill(daInviteeEmail);
        await page.getByRole("button", { name: "Send invitation" }).click();
        await expect(page.getByRole("dialog", { name: "Inviter bruger" })).not.toBeVisible();

        const mail = await fetchLatestMailByRecipient(daInviteeEmail);

        expect(mail.subject).toContain("inviteret til at deltage i");
        expect(mail.subject).toContain(productName);
        expect(mail.html).toContain(`har inviteret dig til at deltage i ${productName}`);
        expect(mail.html).toContain(daInviteeEmail);
        expect(mail.text).toContain(`har inviteret dig til at deltage i ${productName}`);
        expect(mail.text).toContain(daInviteeEmail);
      }
    )();

    await context.close();
  });
});

test.describe("@slow", () => {
  const requestNewCodeTimeout = 31_000; // 31 seconds to ensure button appears
  const sessionTimeout = requestNewCodeTimeout + 30_000;

  /**
   * Localized resend OTP email tests.
   *
   * Covers:
   * - en-US and da-DK resend OTP email content (subject, HTML, plaintext, iOS autofill suffix)
   */
  test("should send localized resend OTP emails after requesting new code", async ({ browser }) => {
    test.setTimeout(sessionTimeout);
    const context = await browser.newContext();
    const page = await context.newPage();
    const testContext = createTestContext(page);

    // === EN-US RESEND OTP ===
    const enResendUser = testUser();

    await step("Sign up en-US user & wait for resend button to appear")(async () => {
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "Email" }).fill(enResendUser.email);
      await page.getByRole("button", { name: "Sign up with email" }).click();
      await expect(page).toHaveURL("/signup/verify");

      await page.waitForTimeout(requestNewCodeTimeout);
      await expect(page.getByRole("button", { name: "Request a new code" })).toBeVisible();
    })();

    await step("Request new code & fetch en-US resend OTP email from Mailpit & assert localized content")(async () => {
      await page.getByRole("button", { name: "Request a new code" }).click();
      await expectToastMessage(testContext, "A new verification code has been sent to your email.");

      const mail = await fetchLatestMailByRecipient(enResendUser.email);

      expect(mail.subject).toBe("Your verification code (resend)");
      expect(mail.html).toContain("Here's your new verification code");
      expect(mail.html).toContain("We're sending this code again as you requested.");
      expect(mail.html).toContain("This code will expire in a few minutes.");
      expect(mail.text).toContain("Here's your new verification code");
      expect(mail.text).toContain("We're sending this code again as you requested.");
      expect(mail.text).toContain("This code will expire in a few minutes.");
      expect(mail.text).toContain("@app.dev.localhost #");
    })();

    // === DA-DK RESEND OTP ===
    const daResendUser = testUser();

    await step("Sign up da-DK user & wait for resend button to appear")(async () => {
      await page.evaluate(() => localStorage.setItem("preferred-locale", "da-DK"));
      await page.goto("/signup");
      await page.getByRole("textbox", { name: "E-mail" }).fill(daResendUser.email);
      await page.getByRole("button", { name: "Tilmeld dig med e-mail" }).click();
      await expect(page).toHaveURL("/signup/verify");

      await page.waitForTimeout(requestNewCodeTimeout);
      await expect(page.getByRole("button", { name: "Anmod om en ny kode" })).toBeVisible();
    })();

    await step("Request new code in da-DK & fetch resend OTP email & assert localized content")(async () => {
      await page.getByRole("button", { name: "Anmod om en ny kode" }).click();
      await expectToastMessage(testContext, "En ny bekræftelseskode er blevet sendt til din e-mail.");

      const mail = await fetchLatestMailByRecipient(daResendUser.email);

      expect(mail.subject).toBe("Din bekræftelseskode (gensendt)");
      expect(mail.html).toContain("Her er din nye bekræftelseskode");
      expect(mail.html).toContain("Vi sender denne kode igen, som du anmodede om.");
      expect(mail.html).toContain("Denne kode udløber om få minutter.");
      expect(mail.text).toContain("Her er din nye bekræftelseskode");
      expect(mail.text).toContain("Vi sender denne kode igen, som du anmodede om.");
      expect(mail.text).toContain("Denne kode udløber om få minutter.");
      expect(mail.text).toContain("@app.dev.localhost #");
    })();

    await context.close();
  });
});
