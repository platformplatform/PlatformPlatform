/// <reference types="node" />
import { defineConfig } from "@playwright/test";
import baseConfig from "../../../shared-webapp/tests/e2e/playwright.config";

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  ...baseConfig,
  testDir: ".",
  testMatch: "**/*.spec.ts"
});
