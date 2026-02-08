---
trigger: glob
globs: **/tests/e2e/**
description: Rules for end-to-end tests
---

# End-to-End Tests

These rules outline the structure, patterns, and best practices for writing end-to-end tests.

## Implementation

1. Use the **end-to-end MCP tool** to run end-to-end tests with these options:
   - Test filtering: smoke tests only, specific browser, search terms
   - Change scoping: last failed tests, only changed tests
   - Flaky test detection: repeat tests, retry on failure, stop on first failure
   - Performance: debug timing to see step execution times
   - **Note**: The **end-to-end MCP tool** always runs with quiet mode automatically

2. Test Search and Filtering:
   - Search by test tags: smoke, comprehensive
   - Search by test content: find tests containing specific text
   - Search by filename: find specific test files
   - Multiple search terms: `end-to-end(searchTerms=["user", "management"])`
   - The tool automatically detects which self-contained systems contain matching tests and only runs those

3. Test-Driven Debugging Process:
   - Focus on one failing test at a time and make it pass before moving to the next
   - Ensure tests use Playwright's built-in auto-waiting assertions: `toHaveURL()`, `toBeVisible()`, `toBeEnabled()`, `toHaveValue()`, `toContainText()`
   - Consider if root causes can be fixed in the application code—fix application bugs rather than masking them with test workarounds

4. Organize tests in a consistent file structure:
   - All e2e test files must be located in `[self-contained-system]/WebApp/tests/e2e/` folder (e.g., `application/account-management/WebApp/tests/e2e/`)
   - All test files use the `*-flows.spec.ts` naming convention (e.g., `login-flows.spec.ts`, `signup-flows.spec.ts`, `user-management-flows.spec.ts`)
   - One test file per feature with max 2 tests: one @smoke and one @comprehensive—prefer extending existing tests even for new features to optimize test speed
   - Top-level describe blocks must use only these 3 approved tags: `test.describe("@smoke", () => {})`, `test.describe("@comprehensive", () => {})`, `test.describe("@slow", () => {})`
   - `@smoke` tests:
     - Critical tests run on deployment of any self-contained system
     - Should be comprehensive scenarios that test core user journeys
     - Keep tests focused on specific flows to reduce fragility while maintaining coverage
     - Focus on must-work functionality with extensive validation steps
     - Include boundary cases and error handling within the same test scenario
     - Avoid testing the same functionality multiple times across different tests
   - `@comprehensive` tests:
     - Thorough tests run when a specific self-contained system is deployed
     - Focus on edge cases, error conditions, and less common scenarios
     - Test specific features in depth with various input combinations
     - Include tests for concurrency, validation rules, accessibility, etc.
     - Group related edge cases together to reduce test count while maintaining coverage
   - `@slow` tests:
     - Optional and run only ad-hoc using `--include-slow` flag
     - Any tests that require waiting like `waitForTimeout` (e.g., for OTP timeouts) must be marked as `@slow`
     - Include tests for rate limiting with actual wait times, session timeouts, etc.
     - Use `test.setTimeout()` at the individual test level based on actual wait times needed

5. Write clear test descriptions and documentation:
   - Test descriptions must accurately reflect what the test covers and be kept in sync with test implementation
   - Use descriptive test names that clearly indicate the functionality being tested (e.g., "should handle single and bulk user deletion workflows with dashboard integration")
   - Include JSDoc comments above complex tests listing all major features/scenarios covered
   - When adding new functionality to existing tests, update both the test description and JSDoc comments to reflect changes

6. Structure each test with step wrappers and proper monitoring:
   - All tests must start with `const context = createTestContext(page);` for proper error monitoring
   - Use step wrappers: `await step("Complete signup & verify account creation")(async () => { /* test logic */ })();`
   - Step naming conventions:
     - Always follow "[Business action + details] & [expected outcome]" pattern
     - Use business action verbs like "Sign up", "Login", "Invite", "Rename", "Update", "Delete", "Create", "Submit"
     - Never use test/assertion prefixes like "Test", "Verify", "Check", "Validate", "Ensure"—use descriptive business actions instead
     - Every step must include an action (arrange/act) followed by assertions, not pure assertion steps
   - Step structure:
     - Use blank lines to separate arrange/act/assert sections within steps
     - Keep shared variable declarations outside steps when used across multiple steps
     - Use section headers with `// === SECTION NAME ===` to group related steps
     - Add JSDoc comments for complex test workflows
   - Use semantic selectors: `page.getByRole("button", { name: "Submit" })`, `page.getByText("Welcome")`, `page.getByLabel("Email")`
   - Assert side effects immediately after actions using `expectToastMessage`, `expectValidationError`, `expectNetworkErrors`
   - Form validation pattern: Use `await blurActiveElement(page);` when updating a textbox the second time before submitting a form to trigger validation

7. Timeout Configuration:
   - Use Playwright's built-in auto-waiting assertions: `toHaveURL()`, `toBeVisible()`, `toBeEnabled()`, `toHaveValue()`, `toContainText()`
   - Don't add timeouts to `.click()`, `.waitForSelector()`, etc.
   - Global timeout configuration is handled in the shared Playwright—don't change this

8. Dropdown Menu Animation Handling:
   - When clicking dropdown menu items (DropdownMenu, user profile menu, etc.), use `.dispatchEvent("click")` instead of regular `.click()`
   - This is required because the DropdownMenu component has a 100ms animation and under parallel load Firefox doesn't reliably process regular clicks
   - Pattern: Click trigger button, wait for menu visible, click menu item with dispatchEvent, verify menu closes
   - Always wait for the menu to be visible before clicking: `await expect(page.getByRole("menu")).toBeVisible();`
   - Don't use `waitForTimeout()` for menu animations - it causes focus loss which closes the dropdown

9. Write deterministic tests for reliable testing:
   - Each test should have a clear, linear flow of actions and assertions
   - Don't use if statements, custom error handling, or try/catch blocks in tests
   - Don't use regular expressions in tests—use simple string matching instead

10. What to test:
    - Enter invalid values such as empty strings, only whitespace characters, long strings, negative numbers, Unicode, etc.
    - Tooltips, keyboard navigation, accessibility, validation messages, translations, responsiveness, etc.

11. Test Fixtures and Page Management:
    - Use appropriate fixtures: `{ page }` for basic tests, `{ anonymousPage }` for tests with existing tenant/owner but not logged in, `{ ownerPage }`, `{ adminPage }`, `{ memberPage }` for authenticated tests
    - Destructure anonymous page data: `const { page, tenant } = anonymousPage; const existingUser = tenant.owner;`
    - Pre-logged in users (`ownerPage`, `adminPage`, `memberPage`) are isolated between workers and will not conflict between tests
    - When using pre-logged in users, do not put the tenant or user into an invalid state that could affect other tests

12. Test Data and Constants:
    - Use underscore separators: `const timeout = 30_000; // 30 seconds`
    - Generate unique data: `const email = uniqueEmail();`
    - Use faker.js to generate realistic test data: `const firstName = faker.person.firstName(); const email = faker.internet.email();`
    - Long string testing: `const longEmail = \`${"a".repeat(90)}@example.com\`; // 101 characters total`

13. Memory Management in End-to-End Tests:
    - Playwright automatically handles browser context cleanup after tests
    - Manual cleanup steps are unnecessary—focus on test clarity over micro-optimizations
    - End-to-End test suites have minimal memory leak concerns due to their limited scope and duration

## Examples

### Dropdown Menu Clicks
```typescript
// ✅ DO: Use dispatchEvent for menu item clicks
await page.getByRole("button", { name: "User profile menu" }).click();
const menu = page.getByRole("menu");
await expect(menu).toBeVisible();
const menuItem = page.getByRole("menuitem", { name: "Log out" });
await expect(menuItem).toBeVisible();
await menuItem.dispatchEvent("click");

// ❌ DON'T: Use waitForTimeout - causes focus loss and menu closure
await page.getByRole("button", { name: "User profile menu" }).click();
const menu = page.getByRole("menu");
await expect(menu).toBeVisible();
await page.waitForTimeout(200); // Focus is lost, menu closes during wait
await page.getByRole("menuitem", { name: "Log out" }).click(); // Fails - menu is closed
```

### ✅ Good Step Naming Examples
```typescript
// ✅ DO: Business action + details & expected outcome
await step("Submit invalid email & verify validation error")(async () => {
  await page.getByLabel("Email").fill("invalid-email");
  await blurActiveElement(page);
  
  await expectValidationError(context, "Invalid email.");
})();

await step("Sign up with valid credentials & verify account creation")(async () => {
  await page.getByRole("button", { name: "Submit" }).click();
  
  await expect(page.getByText("Welcome")).toBeVisible();
})();

await step("Update user role to admin & verify permission change")(async () => {
  const userRow = page.locator("tbody tr").first();

  await userRow.getByLabel("User actions").click();
  const menu = page.getByRole("menu");
  await expect(menu).toBeVisible();
  await page.getByRole("menuitem", { name: "Change role" }).dispatchEvent("click");

  await expect(page.getByRole("alertdialog", { name: "Change user role" })).toBeVisible();
})();
```

### ❌ Bad Step Naming Examples
```typescript
// ❌ DON'T: Pure assertion steps without actions
await step("Verify button is visible")(async () => {
  await expect(page.getByRole("button")).toBeVisible(); // No action, only assertion
})();

// ❌ DON'T: Using test/assertion prefixes
await step("Check user permissions")(async () => { // "Check" is assertion prefix
  await expect(page.getByText("Admin")).toBeVisible();
})();

await step("Validate form state")(async () => { // "Validate" is assertion prefix
  await expect(page.getByRole("textbox")).toBeEmpty();
})();

await step("Ensure user is deleted")(async () => { // "Ensure" is assertion prefix
  await expect(page.getByText("user@example.com")).not.toBeVisible();
})();
```

### ✅ Complete Test Example
```typescript
import { step } from "@shared/e2e/utils/test-step-wrapper";
import { expectValidationError, blurActiveElement, createTestContext } from "@shared/e2e/utils/test-assertions";
import { testUser } from "@shared/e2e/utils/test-data";

test.describe("@smoke", () => {
  test("should complete signup with validation", async ({ page }) => {
    const context = createTestContext(page);
    const user = testUser();

    await step("Submit invalid email & verify validation error")(async () => {
      await page.goto("/signup");
      await page.getByLabel("Email").fill("invalid-email");
      await blurActiveElement(page); // ✅ DO: Trigger validation when updating textbox second time

      await expectValidationError(context, "Invalid email.");
    })();

    await step("Sign up with valid email & verify verification redirect")(async () => {
      await page.getByLabel("Email").fill(user.email);
      await page.getByRole("button", { name: "Continue" }).click();

      await expect(page).toHaveURL("/verify");
    })();
  });
});

test.describe("@comprehensive", () => {
  test("should handle user management with pre-logged owner", async ({ ownerPage }) => {
    createTestContext(ownerPage); // ✅ DO: Create context for pre-logged users

    await step("Access user management & verify owner permissions")(async () => {
      await ownerPage.getByRole("button", { name: "Users" }).click();

      await expect(ownerPage.getByRole("heading", { name: "Users" })).toBeVisible();
    })();
  });
});

test.describe("@slow", () => {
  const requestNewCodeTimeout = 30_000; // 30 seconds
  const codeValidationTimeout = 60_000; // 5 minutes
  const sessionTimeout = codeValidationTimeout + 60_000; // 6 minutes

  test("should handle user logout after too many login attempts", async ({ page }) => { // ✅ DO: use new page, when testing e.g. account lockout
    test.setTimeout(sessionTimeout); // ✅ DO: Set timeout based on actual wait times
    const context = createTestContext(page);

    // ...

    await step("Wait for code expiration & verify timeout behavior")(async () => {
      await page.goto("/login/verify");
      await page.waitForTimeout(codeValidationTimeout); // ✅ DO: Use actual waits in @slow tests

      await expect(page.getByText("Your verification code has expired")).toBeVisible();
    })();
  });
});
```

```typescript
test.describe("@security", () => { // ❌ DON'T: Invent new tags - use @smoke, @comprehensive, @slow only
  test("should handle login", async ({ page }) => {
    // ❌ DON'T: Skip createTestContext(page); step
    page.setDefaultTimeout(5000); // ❌ DON'T: Set timeouts manually - use global config
    
    // ❌ DON'T: Use test/assertion prefixes in step descriptions
    await step("Test login functionality")(async () => { // ❌ Should be "Submit login form & verify authentication"
    await step("Verify button is visible")(async () => { // ❌ Should be "Navigate to page & verify button is visible"
    await step("Check user permissions")(async () => { // ❌ Should be "Click user menu & verify permissions"
      if (page.url().includes("/login/verify")) { // ❌ DON'T: Add conditional logic - tests should be linear
        await page.waitForTimeout(2000); // ❌ DON'T: Add manual timeouts
        // Continue with verification... // ❌ DON'T: Write verbose explanatory comments
      }
      
      await page.click("#submit-btn"); // ❌ DON'T: Use CSS selectors - use semantic selectors
      
      // ❌ DON'T: Skip assertions for side effects
    })();

    // ❌ DON'T: Use regular expressions - use simple string matching instead
    await expect(page.getByText(/welcome.*home/i)).toBeVisible(); // ❌ Should be: page.getByText("Welcome home")
    await expect(page.locator('input[name*="email"]')).toBeFocused(); // ❌ Should be: page.getByLabel("Email")
  });

  // ❌ DON'T: Place assertions outside test functions
  expect(page.url().includes("/admin") || page.url().includes("/login")).toBeTruthy(); // ❌ DON'T: Use ambiguous assertions

  // ❌ DON'T: Use try/catch to handle flaky behavior - makes tests unreliable
  try {
    await page.waitForLoadState("networkidle"); // ❌ DON'T: Add timeout logic in tests
    await page.getByRole("button", { name: "Submit" }).click({ timeout: 1000 }); // ❌ DON'T: Add timeouts to actions
  } catch (error) {
    await page.waitForTimeout(1000); // ❌ DON'T: Add manual waits
    console.log("Retrying..."); // ❌ DON'T: Add custom error handling
  }
});

// ❌ DON'T: Create tests without proper organization
test("isolated test without describe block", async ({ page }) => {
  // ❌ Violates organization rules
});
```
