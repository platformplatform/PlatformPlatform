import { faker } from "@faker-js/faker";
import { expect, type Page } from "@playwright/test";
import { isLocalhost } from "./constants";
import type { TestContext } from "./test-assertions";
import { typeOneTimeCode } from "./test-assertions";

/**
 * Generate a unique email with timestamp to ensure uniqueness
 * @returns Unique email address
 */
export function uniqueEmail(): string {
  // Compact timestamp (YY-MM-DDTHH-MM)
  const timestamp = new Date().toISOString().slice(2, 16).replace(/[-:T]/g, "");

  const username = faker.internet.username().toLowerCase();
  return `${username}@${timestamp}.local`;
}

/**
 * Generate a random first name
 * @returns Random first name
 */
export function firstName(): string {
  return faker.person.firstName();
}

/**
 * Generate a random last name
 * @returns Random last name
 */
export function lastName(): string {
  return faker.person.lastName();
}

/**
 * Generate a random job title
 * @returns Random job title
 */
export function jobTitle(): string {
  return faker.person.jobTitle();
}

/**
 * Generate a random company name
 * @returns Random company name
 */
export function companyName(): string {
  return faker.company.name();
}

/**
 * Get the correct verification code for the current environment
 * @returns "UNLOCK" for local development, or instructions for CI
 */
export function getVerificationCode(): string {
  // For local development, always use "UNLOCK"
  if (isLocalhost()) {
    return "UNLOCK";
  }

  // For CI/CD environments, we'll need to implement email checking
  // For now, throw an error to remind us to implement this
  throw new Error("CI/CD verification code retrieval not yet implemented. Need to check email service.");
}

/**
 * Generate test user data
 * @returns Complete user data object
 */
export function testUser() {
  const first = firstName();
  const last = lastName();
  // Compact timestamp (YY-MM-DDTHH-MM)
  const timestamp = new Date().toISOString().slice(2, 16).replace(/[-:T]/g, "");
  const email = `${first.toLowerCase()}.${last.toLowerCase()}@${timestamp}.local`;

  return {
    email,
    firstName: first,
    lastName: last,
    fullName: `${first} ${last}`,
    jobTitle: jobTitle(),
    company: companyName()
  };
}

/**
 * Log in as Admin via MockEasyAuth on the back-office host.
 *
 * Selects the Admin radio, submits the login form, and waits for the back-office
 * page to land on its expected URL. The caller is responsible for navigating to
 * the desired back-office route before calling this helper, since MockEasyAuth
 * redirects to whichever back-office URL initiated the auth challenge.
 *
 * @param backOfficePage Playwright page bound to the back-office baseURL
 * @param expectedUrl The full back-office URL that should be active after login
 */
export async function logInAsAdmin(backOfficePage: Page, expectedUrl: string): Promise<void> {
  await expect(backOfficePage.getByRole("radio", { name: "Admin Log in with admin rights" })).toBeVisible();
  await backOfficePage.getByRole("radio", { name: "Admin Log in with admin rights" }).click();
  await backOfficePage.getByRole("button", { name: "Log in" }).click();

  await expect(backOfficePage).toHaveURL(expectedUrl);
}

/**
 * Complete the full signup flow for a user
 * @param page Playwright page instance
 * @param expect Playwright expect function
 * @param user User data object with email, firstName, lastName
 * @param keepUserLoggedIn Whether to keep the user logged in after signup (default: true)
 * @param context Test context for asserting toasts
 * @returns Promise that resolves when signup is complete
 */
export async function completeSignupFlow(
  page: Page,
  expect: typeof import("@playwright/test").expect,
  user: { email: string; firstName: string; lastName: string },
  context: TestContext,
  keepUserLoggedIn = true
): Promise<void> {
  // Step 1: Navigate directly to signup page
  await page.goto("/signup");

  // Wait for the page to be fully loaded
  await page.waitForLoadState("domcontentloaded");

  await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

  // Step 2: Enter email and submit
  await page.getByRole("textbox", { name: "Email" }).fill(user.email);
  await page.getByRole("button", { name: "Sign up with email" }).click();
  await expect(page).toHaveURL("/signup/verify");

  // Step 3: Enter verification code (auto-submits after 6 characters)
  await typeOneTimeCode(page, getVerificationCode());
  await expect(page).toHaveURL(/\/welcome/);

  // Step 4: Complete welcome flow - account setup (Owner only)
  await expect(page.getByRole("heading", { name: "Let's set up your account" })).toBeVisible();
  await page.getByRole("textbox", { name: "Account name" }).fill("Test Organization");
  await page.getByRole("button", { name: "Continue" }).click();

  // Step 5: Complete welcome flow - profile setup
  await expect(page.getByRole("heading", { name: "Let's set up your profile" })).toBeVisible();
  await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
  await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
  await page.getByRole("button", { name: "Continue" }).click();

  // Step 6: Verify redirect to dashboard after welcome flow
  await expect(page).toHaveURL("/dashboard");
    await expect(page.getByRole("heading", { name: "Your dashboard is empty" })).toBeVisible();

  // Step 6: Logout if requested (useful for login flow tests)
  if (!keepUserLoggedIn) {
    // Click trigger with JavaScript evaluate to ensure reliable opening on Firefox
    const triggerButton = page.getByRole("button", { name: "User menu" });
    await triggerButton.evaluate((el: HTMLElement) => el.click());

    const accountMenu = page.getByRole("menu");
    await expect(accountMenu).toBeVisible();

    // Click menu item with JavaScript evaluate to bypass stability check during animation
    const logoutMenuItem = page.getByRole("menuitem", { name: "Log out" });
    await expect(logoutMenuItem).toBeVisible();
    await logoutMenuItem.evaluate((el: HTMLElement) => el.click());

    await expect(accountMenu).not.toBeVisible();
    await expect(page).toHaveURL("/login?returnPath=%2Fdashboard");
  }
}
