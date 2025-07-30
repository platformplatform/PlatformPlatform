import type { ConsoleMessage, Page } from "@playwright/test";
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
        "downloadable font: glyf:" // Firefox font loading failures
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
  const toastRegionSelector = '[role="region"]';
  const timeoutMs = 3000;

  // Determine if we have status + message or just message
  const hasStatus = expectedMessage !== undefined;
  const status = hasStatus ? statusOrMessage : undefined;
  const message = hasStatus ? expectedMessage : String(statusOrMessage);

  // Wait for and validate toast message
  const toastLocator = page
    .locator(`${toastRegionSelector} div[class*="whitespace-pre-line"]:has-text("${message}")`)
    .first();

  try {
    await toastLocator.waitFor({ timeout: timeoutMs });
  } catch (error) {
    // If expected toast wasn't found, provide helpful error with actual toasts
    const actualToasts = await checkUnexpectedToasts(context);
    const totalToastCount = await page.locator(`${toastRegionSelector} > div`).count();

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
    const totalToastCount = await page.locator(`${toastRegionSelector} > div`).count();
    throw new Error(
      `Expected exactly 1 toast with message "${message}", but found ${totalToastCount} toasts total.
Expected: 1, Unexpected: ${allToasts.length}
Unexpected toasts: ${allToasts.map((t) => `"${t}"`).join(", ")}
Ensure tests assert all expected toasts or fix the root cause.`
    );
  }

  // Close the toast
  try {
    const toastContainer = page.locator(`${toastRegionSelector} > div`).filter({ hasText: message }).last();
    const closeButton = toastContainer.locator('button[aria-label="Close"]').first();
    if (await closeButton.isVisible()) {
      await closeButton.click();
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
  const { monitoring } = context;

  for (const statusCode of expectedStatusCodes) {
    const expectedNetworkError = `HTTP ${statusCode}`;

    // Find matching network errors
    const matchingErrors = monitoring.networkErrors.filter((error) => error.includes(expectedNetworkError));

    if (matchingErrors.length === 0) {
      throw new Error(
        `Expected network error "${expectedNetworkError}" not found. Actual errors: [${monitoring.networkErrors.join(", ")}]`
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
    const toastRegionSelector = '[role="region"]';

    // Quick non-blocking check - don't wait if no toasts exist
    const toastRegions = page.locator(toastRegionSelector);
    const regionCount = await toastRegions.count();

    if (regionCount === 0) {
      // DEBUG: If no role="region" found, let's look for other potential toast selectors
      const debugToasts: string[] = await page.evaluate(() => {
        const toasts: string[] = [];

        // Look for ANY element that might contain toast text patterns
        document.querySelectorAll('*').forEach(el => {
          const text = el.textContent?.trim();
          if (text && text.length > 0) {
            // Check for success/deletion patterns
            if (text.includes('deleted successfully') ||
                text.includes('User deleted') ||
                text.includes('Success') ||
                (text.includes('success') && text.includes('delete'))) {

              // Make sure element is visible
              const rect = el.getBoundingClientRect();
              if (rect.width > 0 && rect.height > 0) {
                toasts.push(text);
              }
            }
          }
        });

        // Also specifically look for common toast class patterns
        document.querySelectorAll('[class*="toast"], [class*="success"], [class*="notification"], [data-testid*="toast"], [role="status"], [role="alert"]').forEach(el => {
          const text = el.textContent?.trim();
          if (text && text.length > 0) {
            const rect = el.getBoundingClientRect();
            if (rect.width > 0 && rect.height > 0) {
              toasts.push(text);
            }
          }
        });

        return toasts;
      });

      // If we found potential toasts with alternative selectors, add them
      if (debugToasts.length > 0) {
        debugToasts.forEach(toast => {
          if (!expectedMessage || !toast.includes(expectedMessage)) {
            unexpectedToasts.push(toast);
          }
        });
      }

      return unexpectedToasts;
    }

    // Get all current toast messages
    const toastElements = await page.locator(`${toastRegionSelector} > div`).all();

    for (const toastElement of toastElements) {
      try {
        const descriptionElement = toastElement.locator('div[class*="whitespace-pre-line"]').first();
        const descriptionText = await descriptionElement.textContent();

        if (descriptionText?.trim()) {
          const toastMessage = descriptionText.trim();
          // Exclude the currently expected message
          if (!expectedMessage || !toastMessage.includes(expectedMessage)) {
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
