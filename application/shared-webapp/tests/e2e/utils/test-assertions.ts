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
 * Options for assertToastMessage function
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
  return { page, monitoring };
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
      const url = response.url();

      // Filter out expected network errors in test environment
      const expectedNetworkErrors = [
        "/apple-touch-icon.png", // Common 404 in browsers
        "/favicon.ico" // Common 404 in browsers
      ];

      const isExpected = expectedNetworkErrors.some((expected) => url.includes(expected));
      if (!isExpected) {
        results.networkErrors.push(`${response.request().method()} ${response.url()} - HTTP ${response.status()}`);
      }
    }
  });

  // Poll for toast messages to capture all toasts that appear
  const toastPollingInterval = setInterval(async () => {
    try {
      const currentToasts = await captureToastMessages(page, 500);
      for (const toast of currentToasts) {
        // Check if this exact toast (including Reference ID) has already been captured or asserted
        if (!results.toastMessages.includes(toast) && !results.assertedToasts.includes(toast)) {
          results.toastMessages.push(toast);
        }
      }
    } catch {
      // Ignore polling errors
    }
  }, 250);

  // Store the interval ID so we can clear it later
  results.toastPollingInterval = toastPollingInterval;

  return results;
}

/**
 * Assert that EXACTLY ONE toast message occurred
 * @param context Test context containing page and monitoring
 * @param statusOrMessage The expected HTTP status code (e.g., 400, 403, 409) or just the message
 * @param expectedMessage The expected toast message text (can be partial match) - optional if first param is the message
 * @param options Options for assertion behavior:
 *   - expectNetworkError: Whether to expect network error (default: true)
 */
export async function assertToastMessage(
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

  try {
    // First wait for the toast to appear in the UI
    await page.waitForSelector(toastRegionSelector, { timeout: timeoutMs });

    // Wait for toast with expected message to appear using locator
    const toastLocator = page
      .locator(`${toastRegionSelector} div[class*="whitespace-pre-line"]:has-text("${message}")`)
      .first();
    await toastLocator.waitFor({ timeout: timeoutMs });

    // Then wait for the toast to be added to monitoring
    const startTime = Date.now();
    while (Date.now() - startTime < timeoutMs) {
      const unassertedMatches = monitoring.toastMessages.filter(
        (toast) => toast.includes(message) && !monitoring.assertedToasts.includes(toast)
      );

      if (unassertedMatches.length > 0) {
        const matchingToast = unassertedMatches[0];
        monitoring.assertedToasts.push(matchingToast);
        break;
      }

      await page.waitForTimeout(100);
    }
  } catch {
    throw new Error(`Expected toast message containing "${message}" not found within ${timeoutMs}ms`);
  }

  if (hasStatus && options.expectNetworkError) {
    const expectedStatusCode = typeof status === "number" ? status : status === "Forbidden" ? 403 : 400;
    monitoring.expectedStatusCodes.push(expectedStatusCode);
  }
}

/**
 * Assert that a validation error message is visible on the page and automatically clean up 400 network errors
 * @param context Test context containing page and monitoring
 * @param expectedMessage The expected validation error message text (can be partial match)
 */
export async function assertValidationError(context: TestContext, expectedMessage: string): Promise<void> {
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
 * Assert that specific network errors occurred and remove them from monitoring
 * @param context Test context containing page and monitoring
 * @param expectedStatusCodes Array of expected HTTP status codes (e.g., [401, 403])
 */
export async function assertNetworkErrors(context: TestContext, expectedStatusCodes: number[]): Promise<void> {
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

  // Handle any expected network errors
  if (monitoring.expectedStatusCodes.length > 0) {
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

  // Always ignore tracking endpoint errors
  monitoring.networkErrors = monitoring.networkErrors.filter(
    (error) => !error.includes("POST https://localhost:9000/api/track - HTTP 400")
  );

  // Check for unexpected network errors
  const hasUnexpectedNetworkErrors = monitoring.networkErrors.length > 0;

  // Check for unasserted toast messages
  const unassertedToasts = monitoring.toastMessages.filter((toast) => !monitoring.assertedToasts.includes(toast));
  const hasUnexpectedToasts = unassertedToasts.length > 0;

  // If we have any unexpected issues, create a helpful error message
  if (hasUnexpectedNetworkErrors || hasUnexpectedToasts) {
    const errorParts: string[] = [];

    errorParts.push("🎭 Test completed successfully, BUT unexpected issues were detected:");
    errorParts.push(""); // Empty line for readability

    if (hasUnexpectedNetworkErrors) {
      errorParts.push("❌ UNEXPECTED NETWORK ERRORS:");
      monitoring.networkErrors.forEach((error) => {
        errorParts.push(`   ${error}`);
      });
      errorParts.push("");
      errorParts.push("💡 Solutions:");
      errorParts.push("   • If this error is expected, use: assertNetworkErrors(context, [statusCode])");
      errorParts.push("   • If this is a bug, fix the root cause in the application");
      errorParts.push("");
    }

    if (hasUnexpectedToasts) {
      errorParts.push("🍞 UNEXPECTED TOAST MESSAGES:");
      unassertedToasts.forEach((toast) => {
        errorParts.push(`   "${toast}"`);
      });
      errorParts.push("");
      errorParts.push("💡 Solutions:");
      errorParts.push('   • If this toast is expected, use: assertToastMessage(context, "message")');
      errorParts.push("   • If this toast shouldn't appear, fix the root cause in the application");
      errorParts.push("");
    }

    throw new Error(errorParts.join("\n"));
  }
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
