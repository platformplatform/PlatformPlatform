import { faker } from "@faker-js/faker";
import type { Page} from "@playwright/test";
import { isLocalhost } from "./constants";
import type { TestContext } from "./test-assertions";
import { expectToastMessage } from "./test-assertions";

/**
 * Generate a unique email with timestamp to ensure uniqueness
 * @returns Unique email address
 */
export function uniqueEmail(): string {
  // Compact timestamp (YY-MM-DDTHH-MM)
  const timestamp = new Date().toISOString().slice(2, 16).replace(/[-:T]/g, '');

  const username = faker.internet.userName().toLowerCase();
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
  const timestamp = new Date().toISOString().slice(2, 16).replace(/[-:T]/g, '');
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
  await expect(page.getByRole("heading", { name: "Create your account" })).toBeVisible();

  // Step 2: Enter email and submit
  await page.getByRole("textbox", { name: "Email" }).fill(user.email);
  await page.getByRole("button", { name: "Create your account" }).click();
  await expect(page).toHaveURL("/signup/verify");

  // Step 3: Enter verification code
  await page.keyboard.type(getVerificationCode()); // The verification code auto submits

  // Step 4: Complete profile setup and verify successful save
  await page.getByRole("textbox", { name: "First name" }).fill(user.firstName);
  await page.getByRole("textbox", { name: "Last name" }).fill(user.lastName);
  await page.getByRole("button", { name: "Save changes" }).click();
  await expectToastMessage(context, "Success", "Profile updated successfully");

  // Step 5: Wait for successful completion
  await expect(page.getByRole("heading", { name: "Welcome home" })).toBeVisible();

  // Step 6: Logout if requested (useful for login flow tests)
  if (!keepUserLoggedIn) {
    await page.getByRole("button", { name: "User profile menu" }).click();
    await page.getByRole("menuitem", { name: "Log out" }).click();
    await expect(page).toHaveURL("/login?returnPath=%2Fadmin");
  }
}
