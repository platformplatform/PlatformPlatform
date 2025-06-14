/// <reference types="node" />
import { defineConfig, devices } from "@playwright/test";
import { getBaseUrl } from "./utils/constants";

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  // Run tests in files in parallel
  fullyParallel: true,

  // Fail the build on CI if you accidentally left test.only in the source code.
  forbidOnly: !!process.env.CI,

  // Retry on CI only
  retries: process.env.CI ? 2 : 0,

  // Opt out of parallel tests on CI.
  workers: process.env.CI ? 1 : undefined,

  // Reporter to use. See https://playwright.dev/docs/test-reporters
  reporter: process.env.CI ? "github" : [["list"], ["html", { open: "never" }]],

  // Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions.
  use: {
    // Base URL to use in actions like `await page.goto('/')`.
    // biome-ignore lint/style/useNamingConvention: Using Playwright's required property name
    baseURL: getBaseUrl(),

    // Browser launch options
    launchOptions: {
      // Slow motion delay controlled by CLI --slow-mo flag
      slowMo: process.env.PLAYWRIGHT_SLOW_MO ? Number.parseInt(process.env.PLAYWRIGHT_SLOW_MO) : 0
    },

    // Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer
    trace: "on-first-retry",
    // Take screenshot on failure
    screenshot: "only-on-failure",
    // Record video - always use retain-on-failure for better HTML report compatibility
    // Videos will be recorded for failed tests and can be forced on via CLI if needed
    video: process.env.PLAYWRIGHT_VIDEO_MODE === "on" ? "on" : "retain-on-failure"
  },

  // Global timeout for each test (double timeout for slow motion)
  timeout: (() => {
    const baseTimeout = process.env.PLAYWRIGHT_TIMEOUT ? Number.parseInt(process.env.PLAYWRIGHT_TIMEOUT) : 30000;
    const isSlowMotion = !!process.env.PLAYWRIGHT_SLOW_MO;
    return isSlowMotion ? baseTimeout * 2 : baseTimeout;
  })(),
  expect: {
    timeout: 10000
  },

  // Output directories
  outputDir: "test-results/",

  // Configure projects for major browsers
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] }
    },

    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] }
    },

    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] }
    }
  ]
});
