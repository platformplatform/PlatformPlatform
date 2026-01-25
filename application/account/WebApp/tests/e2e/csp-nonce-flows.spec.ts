import { expect } from "@playwright/test";
import { test } from "@shared/e2e/fixtures/page-auth";
import { createTestContext } from "@shared/e2e/utils/test-assertions";
import { step } from "@shared/e2e/utils/test-step-wrapper";

test.describe("@smoke", () => {
  test("should block inline scripts and styles injected without valid nonce", async ({ page }) => {
    createTestContext(page);

    await step("Navigate to landing page & verify CSP nonce configuration")(async () => {
      const response = await page.goto("/");

      await expect(page).toHaveURL("/");

      // Verify meta tag exists
      const nonceMetaExists = await page.locator('meta[name="csp-nonce"]').count();
      expect(nonceMetaExists).toBe(1);

      // Verify CSP headers require nonce for scripts and styles
      const cspHeader = response?.headers()["content-security-policy"];
      expect(cspHeader).toBeTruthy();
      expect(cspHeader).toContain("script-src");
      expect(cspHeader).toContain("'nonce-");
      expect(cspHeader).toContain("style-src");
    })();

    await step("Inject malicious script via innerHTML & verify execution is blocked")(async () => {
      const scriptBlocked = await page.evaluate(() => {
        // Attacker tries to inject script via innerHTML (XSS attack)
        const container = document.createElement("div");
        container.innerHTML = "<script>window.__xssAttack__ = true;</script>";
        const script =
          container.querySelector("script") ??
          (() => {
            throw new Error("Failed to create script element");
          })();
        document.head.appendChild(script);

        // Check if script executed. Should be false (blocked by CSP).
        return !(window as unknown as { __xssAttack__?: boolean }).__xssAttack__;
      });

      expect(scriptBlocked).toBe(true);
    })();

    await step("Inject malicious CSS via innerHTML & verify styles are blocked")(async () => {
      const cssBlocked = await page.evaluate(() => {
        // Attacker tries to inject CSS via innerHTML (XSS attack)
        const container = document.createElement("div");
        container.innerHTML = "<style>body { border: 10px solid red !important; }</style>";
        const style =
          container.querySelector("style") ??
          (() => {
            throw new Error("Failed to create style element");
          })();
        document.head.appendChild(style);

        // Check if malicious CSS was applied. Should NOT have red border (blocked by CSP).
        const border = window.getComputedStyle(document.body).border;
        return !border.includes("10px") || !border.includes("red");
      });

      expect(cssBlocked).toBe(true);
    })();
  });
});
