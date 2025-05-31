---
trigger: glob
globs: */tests/e2e/**
description: Rules for end-to-end tests
---

# End-to-End Tests

These rules outline the structure, patterns, and best practices for writing end-to-end tests.

## Implementation

1. Use `[CLI_ALIAS] e2e` with these option categories to optimize test execution:
   - Test filtering: `--smoke`, `--include-slow`, `--grep`, `--browser`
   - Change scoping: `--last-failed`, `--only-changed`
   - Flaky test detection: `--repeat-each`, `--retries`, `--stop-on-first-failure`

2. Test-Driven Debugging Process:
   - Focus on one failing test at a time and make it pass before moving to the next.
   - Ensure tests use Playwright's built-in auto-waiting assertions: `toHaveURL()`, `toBeVisible()`, `toBeEnabled()`, `toHaveValue()`, `toContainText()`.
   - Consider if root causes can be fixed in the application code, and fix application bugs rather than masking them with test workarounds.
   - Use Browser MCP to manually test the feature and verify it works correctly outside of automated tests.

3. Organize tests in a consistent file structure:
   - One file per feature (e.g., `signup.spec.ts`).
   - Group tests using nested `test.describe` blocks with these 3 tags:
     ```typescript
     test.describe("Feature Name", () => {
       test.describe("@smoke", () => {});
       
       test.describe("@comprehensive", () => {});
       
       test.describe("@slow", () => {
         test.describe.configure({ timeout: 360000 });
       });
     });
     ```
  - `@smoke` tests:
    - Critical tests run on deployment of any self-contained system.
    - Should be very long test scenarios testing all happy paths and selected boundary cases in a few tailored tests.

  - `@comprehensive` tests:
    - Thorough tests run when a specific self-contained system is deployed.
    - Focused on testing a specific area covering all edge cases, e.g., responsive design, keyboard navigation, concurrency, error handling, and validation.

  - `@slow` tests:
    - Optional and run only ad-hoc using `--include-slow` flag.
    - Any tests that require waiting like `waitForTimeout` (e.g., for OTP timeouts) must be marked as `@slow`.

4. Structure each test with clear *steps*, assertions, and proper monitoring:
   - All tests must start with `const context = createTestContext(page);` and end with `assertNoUnexpectedErrors(context);`
   - Create multiple *steps* that all include arrange, act, and assert steps.
   - Use clear, concise *step* comments explaining what (arrange and act) *and* expected result (assert).
   - Use semantic selectors: `page.getByRole("button", { name: "Submit" })`, `page.getByText("Welcome")`, `page.getByLabel("Email")`
   - Assert side effects immediately after an action using `assertToastMessage`, `assertValidationError`, `assertNetworkErrors`.
   - Avoid verbose explanatory comments *within* a step; if needed, add comments inline after statement.

5. Write deterministic tests - This is critical for reliable testing:
   - Each test should have a clear, linear flow of actions and assertions.
   - Never use if statements, custom error handling, or try/catch blocks in tests.
   - Tests should be independent and not rely on state from other tests.

6. What to test:
- Enter invalid values, such as empty strings, only whitespace characters, long strings, negative numbers, Unicode, etc.
   - Tooltips, keyboard navigation, accessibility, validation messages, translations, responsiveness, etc.

## Examples

```typescript
test.describe("@smoke", () => {
    test("should complete full signup flow from homepage to admin dashboard", async ({ page }) => {
    const context = createTestContext(page); // ✅ DO: Always start with this

    // Step 1: Navigate from homepage to signup page and verify
    await page.goto("/");
    await page.getByRole("button", { name: "Signup" }).first().click();
    await expect(page).toHaveURL("/signup"); // ✅ DO: Wait for navigation before proceeding

    // Step 2: Enter credentials and verify validation
    await page.getByLabel("Email").fill("test@example.com");
    await page.keyboard.press("Tab"); // Move to region selector // ✅ DO: Add comments inline when something is unclear
    await page.getByRole("button", { name: "Continue" }).click();
    await assertToastMessage(context, "Success", "Check your email."); // ✅ DO: Wait for side effects before proceeding

    // Step 3: Enter verification code and verify successful login
    await page.keyboard.type(getVerificationCode());
    await page.getByRole("button", { name: "Verify" }).click();
    await expect(page).toHaveURL("/admin"); // ✅ DO: Wait for navigation before final assertions
    await expect(page.getByRole("heading", { name: "Welcome" })).toBeVisible(); // ✅ DO: Wait for content before final assertions

    // Step 4: Assert no unexpected errors occurred // ✅ DO: Always use this exact comment
    assertNoUnexpectedErrors(context);
    });
});
```

```typescript
test.describe("@security", () => { // ❌ DON'T: Don't invent new tags
  test("should handle login", async ({ page }) => {
    // ❌ DON'T: Skip createTestContext(page); step

    // Navigate to login page  // ❌ DON'T: Don't add step comments without "Step #" prefix and expected result   
    if (currentUrl.includes("/login/verify")) { // ❌ DON'T: Add conditional logic in tests
      // Continue with verification... // ❌ DON'T: Don't write verbose explanatory comments
    }
  });

  expect(page.url().includes("/admin") || page.url().includes("/login")).toBeTruthy(); // ❌ DON'T: Use ambiguous assertions

  // ❌ DON'T: Use try/catch to handle flaky behavior
  try {
    await page.waitForLoadState("networkidle"); // ❌ DON'T: Don't add timeout logic in tests
    await page.getByRole("button", { name: "Submit" }).click();
  } catch (error) {
    await page.waitForTimeout(1000); // ❌ DON'T: Don't add timeout logic in tests
    // Fallback logic - this masks real issues!
  }
});

// Step 4: Verify no unexpected errors occurred // ❌ DON'T: Change the default closing comment
assertNoUnexpectedErrors(context);
```
