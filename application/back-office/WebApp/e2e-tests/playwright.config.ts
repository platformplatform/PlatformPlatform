/// <reference types="node" />
import { defineConfig } from "@playwright/test";
import baseConfig from "../../../shared-webapp/e2e-tests/config/playwright.config";

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  ...baseConfig,
  testDir: ".",
  testMatch: "**/*.spec.ts"
});
