import type { ConsoleMessage, Locator, Page } from "@playwright/test";
import { expect } from "@playwright/test";

/**
 * Interface for monitoring results - captures ALL errors/messages for strict assertion
 */
export interface MonitoringResults {
  consoleMessages: ConsoleMessage[];
  networkErrors: string[];
  expectedStatusCodes: number[];
}

/**
 * Test context that holds page and monitoring for simplified function calls
 */
export interface TestContext {
  page: Page;
  monitoring: MonitoringResults;
}

/**
 * Options for expectToastMessage function
 */
interface AssertToastOptions {
  expectNetworkError?: boolean;
}

/**
 * Create a test context with page and monitoring for simplified function calls
 * @param page Playwright page instance
 * @returns Test context with page and monitoring
 */
export function createTestContext(page: Page): TestContext {
  const monitoring = startMonitoring(page);
  const context = { page, monitoring };

  // Store context on page object so afterEach hook can access the same instance
  (page as Page & { __testContext?: TestContext }).__testContext = context;

  return context;
}

/**
 * Internal function to start monitoring console messages, network errors, and toast messages for a page
 * @param page Playwright page instance
 * @returns Monitoring results object that will be populated during test execution
 */
function startMonitoring(page: Page): MonitoringResults {
  const results: MonitoringResults = {
    consoleMessages: [],
    networkErrors: [],
    expectedStatusCodes: []
  };

  // Monitor console errors and warnings with filtering for expected messages
  page.on("console", (consoleMessage) => {
    if (["warning", "error"].includes(consoleMessage.type())) {
      const message = consoleMessage.text();

      // Filter out expected console messages in test environment
      const expectedMessages = [
        "Error with Permissions-Policy header: Unrecognized feature: 'web-share'",
        "If you do not provide a visible label, you must specify an aria-label or aria-labelledby attribute for accessibility",
        "Content-Security-Policy:",
        "MouseEvent.mozInputSource is deprecated",
        "A PressResponder was rendered without a pressable child",
        "WebSocket connection to", // Hot reload/dev server WebSocket connections
        "Refused to connect to ws://", // WebSocket CSP violations from dev servers
        "Refused to connect to wss://", // Secure WebSocket CSP violations from dev servers
        "Loading failed for the <script>", // Firefox/WebKit JS loading failures in dev environment
        "ChunkLoadError: Loading chunk", // Webpack/Rspack chunk loading errors in dev environment
        "Loading chunk", // Additional chunk loading error variations
        "Loading CSS chunk", // CSS chunk loading failures in dev environment
        "Error: Loading CSS chunk", // CSS chunk loading error variations
        "downloadable font: download failed", // Firefox font loading failures
        "downloadable font: glyf:", // Firefox font loading failures
        "ResizeObserver loop completed with undelivered notifications" // Benign browser warning, especially in Firefox
      ];

      const isExpected = expectedMessages.some((expected) => message.includes(expected));
      if (!isExpected) {
        results.consoleMessages.push(consoleMessage);
      }
    }
  });

  // Monitor network errors with filtering for expected errors
  page.on("response", (response) => {
    if (response.status() >= 400) {
      results.networkErrors.push(`${response.request().method()} ${response.url()} - HTTP ${response.status()}`);
    }
  });

  return results;
}

/**
 * Expect that EXACTLY ONE toast message occurred
 * @param context Test context containing page and monitoring
 * @param statusOrMessage The expected HTTP status code (e.g., 400, 403, 409) or just the message
 * @param expectedMessage The expected toast message text (can be partial match) - optional if first param is the message
 * @param options Options for assertion behavior:
 *   - expectNetworkError: Whether to expect network error (default: true)
 */
export async function expectToastMessage(
  context: TestContext,
  statusOrMessage: string | number,
  expectedMessage?: string,
  options: AssertToastOptions = { expectNetworkError: true }
): Promise<void> {
  const { page } = context;
  const toastRegionSelector = '[data-sonner-toaster]';
  const timeoutMs = 3000;

  // Determine if we have status + message or just message
  const hasStatus = expectedMessage !== undefined;
  const status = hasStatus ? statusOrMessage : undefined;
  const message = hasStatus ? expectedMessage : String(statusOrMessage);

  // Wait for and validate toast message (Sonner uses [data-title] for title-only toasts, [data-description] for toasts with separate description)
  const toastLocator = page
    .locator(
      `${toastRegionSelector} li[data-sonner-toast]:has([data-title]:has-text("${message}")), ${toastRegionSelector} li[data-sonner-toast]:has([data-description]:has-text("${message}"))`
    )
    .first();

  try {
    await toastLocator.waitFor({ timeout: timeoutMs });
  } catch (error) {
    // If expected toast wasn't found, provide helpful error with actual toasts
    const actualToasts = await checkUnexpectedToasts(context);
    const totalToastCount = await page.locator(`${toastRegionSelector} li[data-sonner-toast]`).count();

    throw new Error(
      `Expected toast with message "${message}" not found within ${timeoutMs}ms.
Found ${totalToastCount} toast(s) total.
Actual toasts: ${actualToasts.length > 0 ? actualToasts.map((t) => `"${t}"`).join(", ") : "None"}

💡 Solutions:
  • If this toast should appear, check the application logic
  • If the message is different, update the expected message
  • If no toast should appear, remove this assertion

Original error: ${error}`
    );
  }

  // Check for multiple toasts
  const allToasts = await checkUnexpectedToasts(context, message);
  if (allToasts.length > 0) {
    const totalToastCount = await page.locator(`${toastRegionSelector} li[data-sonner-toast]`).count();
    throw new Error(
      `Expected exactly 1 toast with message "${message}", but found ${totalToastCount} toasts total.
Expected: 1, Unexpected: ${allToasts.length}
Unexpected toasts: ${allToasts.map((t) => `"${t}"`).join(", ")}
Ensure tests assert all expected toasts or fix the root cause.`
    );
  }

  // Close the toast and wait for it to disappear
  try {
    const toastContainer = page.locator(`${toastRegionSelector} li[data-sonner-toast]`).filter({ hasText: message }).last();
    const closeButton = toastContainer.locator("button[data-close-button]").first();
    if (await closeButton.isVisible()) {
      await closeButton.click();
      // Wait for toast to be dismissed (Sonner has exit animation)
      await toastContainer.waitFor({ state: "hidden", timeout: 2000 });
    }
  } catch {
    // Toast might have auto-dismissed, which is fine
  }

  if (hasStatus && options.expectNetworkError && typeof status === "number") {
    // Only look for network errors if it's actually an error status code (4xx, 5xx)
    if (status >= 400) {
      // Clean up the network error immediately
      await expectNetworkErrors(context, [status]);
    }
  }
}

/**
 * Expect that a validation error message is visible on the page and automatically clean up 400 network errors
 * @param context Test context containing page and monitoring
 * @param expectedMessage The expected validation error message text (can be partial match)
 */
export async function expectValidationError(context: TestContext, expectedMessage: string): Promise<void> {
  const { monitoring, page } = context;
  const timeoutMs = 3000;

  // Wait for validation error to appear on the page
  try {
    const validationErrorLocator = page.getByText(expectedMessage).first();
    await validationErrorLocator.waitFor({ timeout: timeoutMs });
    await expect(validationErrorLocator).toBeVisible();
  } catch (error) {
    throw new Error(
      `Expected validation error message containing "${expectedMessage}" not found within ${timeoutMs}ms. Error: ${error}`
    );
  }

  // Automatically clean up any 400 network errors (if they exist)
  const http400Error = "HTTP 400";
  const matchingNetworkErrors = monitoring.networkErrors.filter((error) => error.includes(http400Error));

  if (matchingNetworkErrors.length > 0) {
    const errorIndex = monitoring.networkErrors.findIndex((error) => error.includes(http400Error));
    monitoring.networkErrors.splice(errorIndex, 1);

    const consoleErrorMessage = "Failed to load resource: the server responded with a status of 400";
    const consoleIndex = monitoring.consoleMessages.findIndex((msg) => msg.text().includes(consoleErrorMessage));
    if (consoleIndex !== -1) {
      monitoring.consoleMessages.splice(consoleIndex, 1);
    }
  }
}

/**
 * Expect that specific network errors occurred and remove them from monitoring
 * @param context Test context containing page and monitoring
 * @param expectedStatusCodes Array of expected HTTP status codes (e.g., [401, 403])
 */
export async function expectNetworkErrors(context: TestContext, expectedStatusCodes: number[]): Promise<void> {
  const { monitoring, page } = context;

  for (const statusCode of expectedStatusCodes) {
    const expectedNetworkError = `HTTP ${statusCode}`;

    // Poll for the network error with a timeout (since response events may not be processed immediately)
    const startTime = Date.now();
    const timeout = 3000;
    let matchingErrors: string[] = [];

    while (matchingErrors.length === 0 && Date.now() - startTime < timeout) {
      matchingErrors = monitoring.networkErrors.filter((error) => error.includes(expectedNetworkError));
      if (matchingErrors.length === 0) {
        await page.evaluate(() => new Promise((resolve) => setTimeout(resolve, 10)));
      }
    }

    if (matchingErrors.length === 0) {
      throw new Error(
        `Expected network error "${expectedNetworkError}" not found within ${timeout}ms. Actual errors: [${monitoring.networkErrors.join(", ")}]`
      );
    }

    // Remove all matching network errors for this status code
    for (let i = monitoring.networkErrors.length - 1; i >= 0; i--) {
      if (monitoring.networkErrors[i].includes(expectedNetworkError)) {
        monitoring.networkErrors.splice(i, 1);
      }
    }

    // Also remove corresponding console error messages for this HTTP status
    const consoleErrorMessage = `Failed to load resource: the server responded with a status of ${statusCode}`;
    for (let i = monitoring.consoleMessages.length - 1; i >= 0; i--) {
      if (monitoring.consoleMessages[i].text().includes(consoleErrorMessage)) {
        monitoring.consoleMessages.splice(i, 1);
      }
    }
  }
}

/**
 * Assert that ALL queues are completely empty - no unexpected errors or console messages
 * @param context Test context containing page and monitoring
 */
export async function assertNoUnexpectedErrors(context: TestContext): Promise<void> {
  const monitoring = context.monitoring;

  // No polling interval cleanup needed anymore

  // Clean up expected network errors and tracking errors
  cleanupExpectedNetworkErrors(monitoring);

  // Check for any remaining unexpected issues
  const hasUnexpectedNetworkErrors = monitoring.networkErrors.length > 0;

  // Check for unexpected toasts using our new silent checking function
  const unexpectedToasts = await checkUnexpectedToasts(context);
  const hasUnexpectedToasts = unexpectedToasts.length > 0;

  // If we have any unexpected issues, throw a helpful error message
  if (hasUnexpectedNetworkErrors || hasUnexpectedToasts) {
    const errorMessage = buildUnexpectedErrorsMessage(monitoring.networkErrors, unexpectedToasts);
    throw new Error(errorMessage);
  }
}

/**
 * Remove expected network errors from monitoring results
 */
function cleanupExpectedNetworkErrors(monitoring: MonitoringResults): void {
  if (monitoring.expectedStatusCodes.length === 0) {
    return;
  }

  for (const expectedStatusCode of monitoring.expectedStatusCodes) {
    const expectedNetworkError = `HTTP ${expectedStatusCode}`;
    const networkErrors = monitoring.networkErrors.filter((error) => error.includes(expectedNetworkError));

    // Remove all matching network errors (Firefox may create multiple)
    for (const error of networkErrors) {
      const index = monitoring.networkErrors.indexOf(error);
      if (index !== -1) {
        monitoring.networkErrors.splice(index, 1);
      }
    }
  }
}

/**
 * Build a comprehensive error message for unexpected issues
 */
function buildUnexpectedErrorsMessage(networkErrors: string[], unassertedToasts: string[]): string {
  const errorParts: string[] = [];

  errorParts.push("🎭 Test completed successfully, BUT unexpected issues were detected:");
  errorParts.push(""); // Empty line for readability

  if (networkErrors.length > 0) {
    errorParts.push("❌ UNEXPECTED NETWORK ERRORS:");
    networkErrors.forEach((error) => {
      errorParts.push(`   ${error}`);
    });
    errorParts.push("");
    errorParts.push("💡 Solutions:");
    errorParts.push("   • If this error is expected, use: expectNetworkErrors(context, [statusCode])");
    errorParts.push("   • If this is a bug, fix the root cause in the application");
    errorParts.push("");
  }

  if (unassertedToasts.length > 0) {
    errorParts.push("🍞 UNEXPECTED TOAST MESSAGES:");
    unassertedToasts.forEach((toast) => {
      errorParts.push(`   "${toast}"`);
    });
    errorParts.push("");
    errorParts.push("💡 Solutions:");
    errorParts.push('   • If this toast is expected, use: expectToastMessage(context, "message")');
    errorParts.push("   • If this toast shouldn't appear, fix the root cause in the application");
    errorParts.push("");
  }

  return errorParts.join("\n");
}

/**
 * Silently check for unexpected toasts without adding to trace
 * This runs after each step to verify no unexpected toasts appeared
 * @param context Test context containing page and monitoring
 * @param expectedMessage Optional message to exclude from unexpected list (currently being asserted)
 * @returns Array of unexpected toast messages found
 */
export async function checkUnexpectedToasts(context: TestContext, expectedMessage?: string): Promise<string[]> {
  const { page } = context;
  const unexpectedToasts: string[] = [];

  try {
    const toastRegionSelector = "[data-sonner-toaster]";

    // Quick non-blocking check - don't wait if no toasts exist
    const toastRegions = page.locator(toastRegionSelector);
    const regionCount = await toastRegions.count();

    if (regionCount === 0) {
      return unexpectedToasts;
    }

    // Get all current toast messages (Sonner uses li[data-sonner-toast] for each toast)
    const toastElements = await page.locator(`${toastRegionSelector} li[data-sonner-toast]`).all();

    for (const toastElement of toastElements) {
      try {
        // Sonner uses [data-title] for the main message and optionally [data-description] for details
        const titleElement = toastElement.locator("[data-title]");
        const descriptionElement = toastElement.locator("[data-description]");
        const titleText = (await titleElement.count()) > 0 ? (await titleElement.first().textContent())?.trim() : null;
        const descriptionText =
          (await descriptionElement.count()) > 0 ? (await descriptionElement.first().textContent())?.trim() : null;
        const toastMessage = titleText || descriptionText;

        if (toastMessage) {
          // Exclude toast if the expected message matches either title or description
          const isExpected =
            expectedMessage &&
            (toastMessage.includes(expectedMessage) || (descriptionText && descriptionText.includes(expectedMessage)));
          if (!isExpected) {
            unexpectedToasts.push(toastMessage);
          }
        }
      } catch {
        // Element might have been removed, continue
      }
    }
  } catch {
    // No toasts found or error accessing them
  }

  return unexpectedToasts;
}

/**
 * Blur the currently focused element to ensure input values are committed
 *
 * This is a workaround for a Playwright issue where WebKit and Firefox don't properly
 * register input values when a form is submitted immediately after filling an input.
 * Without blurring the input first, these browsers may submit empty or stale values.
 *
 * This issue doesn't occur in real browsers, only in Playwright's automation.
 *
 * @param page The Playwright page instance
 */
export async function blurActiveElement(page: Page): Promise<void> {
  await page.evaluate(() => {
    const element = (globalThis as { document?: { activeElement?: { blur?: () => void } } }).document?.activeElement;
    if (element?.blur) {
      element.blur();
    }
  });
}

/**
 * Select an option from a Select dropdown, handling Firefox animation timing issues.
 *
 * The Select component (Base UI) has a 100ms animation that causes Firefox to report
 * "element is not stable" during the animation. This function:
 * 1. Clicks the trigger to open the dropdown
 * 2. Waits for the popup to be fully open (data-open attribute)
 * 3. Clicks the option with force to bypass stability checks
 * 4. Waits for the popup to close before returning
 *
 * @param trigger The Playwright locator for the Select trigger (e.g., page.getByLabel("User role"))
 * @param page The Playwright page instance
 * @param optionName The name of the option to click (used with getByRole("option", { name }))
 */
export async function selectOption(trigger: Locator, page: Page, optionName: string): Promise<void> {
  // Use dispatchEvent for more reliable click in Firefox under load
  await trigger.dispatchEvent("click");
  const popup = page.locator('[data-slot="select-content"][data-open]');
  await expect(popup).toBeVisible();
  const option = page.getByRole("option", { name: optionName });
  await expect(option).toBeVisible();
  await option.click({ force: true });
  await expect(popup).not.toBeVisible();
}

/**
 * @deprecated Use selectOption instead which handles the full open/select/close sequence
 */
export async function clickSelectOption(page: Page, optionName: string): Promise<void> {
  const popup = page.locator('[data-slot="select-content"][data-open]');
  await expect(popup).toBeVisible();
  const option = page.getByRole("option", { name: optionName });
  await expect(option).toBeVisible();
  await option.click({ force: true });
  await expect(popup).not.toBeVisible();
}

/**
 * Type an OTP verification code into the one-time-code inputs.
 *
 * This function uses the native HTMLInputElement value setter to properly trigger
 * React's onChange handler for the input-otp library. The input-otp library uses
 * a single hidden input element that's controlled by React state.
 *
 * The native setter approach is required because:
 * 1. Direct value assignment (input.value = x) doesn't trigger React's synthetic events
 * 2. The input-otp library relies on React's onChange to update its internal state
 * 3. Using Object.getOwnPropertyDescriptor to get the native setter bypasses React's wrapper
 *
 * Note: We don't verify values after typing because auto-submit may navigate away
 * before verification completes. Tests verify success via navigation or error toasts.
 *
 * @param page The Playwright page instance
 * @param code The verification code to enter (e.g., "UNLOCK", "WRONG1")
 */
export async function typeOneTimeCode(page: Page, code: string): Promise<void> {
  const otpInput = page.locator('input[autocomplete="one-time-code"]');

  // Wait for the OTP input to be focused before typing
  await expect(otpInput).toBeFocused();

  // Type the entire code at once using native value setter to trigger React's onChange
  await page.evaluate((codeToType) => {
    const activeElement = document.activeElement as HTMLInputElement;
    if (!activeElement) return;

    // Get the native HTMLInputElement value setter (bypasses React's controlled input wrapper)
    const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
      window.HTMLInputElement.prototype,
      "value"
    )?.set;

    if (nativeInputValueSetter) {
      // Set the value using native setter
      nativeInputValueSetter.call(activeElement, codeToType.toUpperCase());

      // Dispatch input event to trigger React's onChange handler
      activeElement.dispatchEvent(new Event("input", { bubbles: true }));
    }
  }, code);
}
