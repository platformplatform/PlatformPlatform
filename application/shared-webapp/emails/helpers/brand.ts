import { loadPlatformSettings } from "@repo/build/platformSettings";

import { getEmailRenderMode } from "./renderMode";

// Brand values consumed in places a <Value> component can't reach -- CSS color literals in `style`
// attributes, the <style> block, and plain HTML attributes like `alt`. In "build" mode they emit
// the Scriban placeholder so ScribanEmailRenderer substitutes per request; in "preview" mode they
// resolve to the real platform-settings.jsonc value so the dev preview is brand-accurate.

// The email-header band background. Same color in light- and dark-mode email clients.
export function emailHeaderBackground(): string {
  return getEmailRenderMode() === "build"
    ? "{{EmailHeaderBackground}}"
    : loadPlatformSettings().branding.emailHeaderBackground;
}

// The product name, for attribute contexts (e.g. an `alt`) where <Value> cannot render.
export function emailProductName(): string {
  return getEmailRenderMode() === "build" ? "{{ProductName}}" : loadPlatformSettings().branding.productName;
}
