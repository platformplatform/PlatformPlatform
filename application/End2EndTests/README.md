# End2EndTests

End-to-end tests for PlatformPlatform using Playwright.

## About Playwright

Playwright is Microsoft's modern end-to-end testing framework that provides reliable, fast, and cross-browser testing capabilities. It's the right choice for PlatformPlatform because it offers excellent developer experience with built-in debugging tools, automatic waiting, and comprehensive browser support including Chromium, Firefox, and WebKit.

## Test Organization

Create tests in folders under `tests/your-self-contained-system/` (e.g., `tests/account-management/`, `tests/back-office/`). Use `@smoke` tags for fast, essential tests that should run on every change.

## Prerequisites

- Application running at `https://localhost:9000`
