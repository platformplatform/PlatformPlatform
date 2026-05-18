import fs from "node:fs";
import path from "node:path";

export interface PlatformBranding {
  /** Product/platform name displayed throughout the application */
  productName: string;
  /**
   * PWA toolbar tint per theme mode. The HTML `<meta name="theme-color">` initial value is
   * `themeColor.light`; the frontend's ThemeColorUpdater swaps it to `themeColor.dark` when the
   * resolved theme is dark. The manifest.json `theme_color` is single-valued (PWA spec) and uses
   * `themeColor.light` as the install-time tint. Hex only (PWA rejects oklch).
   */
  themeColor: {
    light: string;
    dark: string;
  };
  /** PWA splash-screen background while the SPA boots (hex, no oklch) */
  backgroundColor: string;
  /** Background color of the brand band at the top of every transactional email */
  emailHeaderBackground: string;
  /**
   * Brand primary color (the "button color"). Each value is a CSS color expression. The frontend
   * pipes them into theme.css via inline `<style>` injected per HTML template; the corresponding
   * `*Foreground` is the text/icon color used on top of the primary.
   */
  primaryColor: {
    light: string;
    lightForeground: string;
    dark: string;
    darkForeground: string;
  };
  /**
   * One-line product description per channel and locale. The frontend reads `tagline.web[locale]`;
   * emails read `tagline.mail[locale]` server-side. Locale keys in `web` and `mail` must match.
   */
  tagline: {
    web: Record<string, string>;
    mail: Record<string, string>;
  };
  /** Contact email shown as a mail icon in the landing page footer (empty = hidden) */
  contactEmail: string;
  /** Support email shown to signed-in users in the in-app support dialog */
  supportEmail: string;
  /** Whether the iOS "Add to Home Screen" install prompt renders in the user-facing app */
  showAddToHomescreen: boolean;
}

export interface PlatformSocialLinks {
  /** Public GitHub profile / repository URL (empty string hides the icon) */
  gitHub: string;
  /** Public LinkedIn profile URL (empty string hides the icon) */
  linkedIn: string;
  /** Public YouTube channel URL (empty string hides the icon) */
  youTube: string;
  /** Public X (formerly Twitter) profile URL (empty string hides the icon) */
  x: string;
  /** Public Facebook page URL (empty string hides the icon) */
  facebook: string;
  /** Public Instagram profile URL (empty string hides the icon) */
  instagram: string;
}

export interface PlatformSettings {
  branding: PlatformBranding;
  socialLinks: PlatformSocialLinks;
}

// platform-settings.jsonc is the single source of truth for brand configuration, shared by the
// backend (embedded resource) and the frontend (injected here at build time). It lives at the
// application root; this file compiles to shared-webapp/build/dist/, three levels below it.
const settingsPath = path.join(__dirname, "..", "..", "..", "platform-settings.jsonc");

export function loadPlatformSettings(): PlatformSettings {
  const raw = fs.readFileSync(settingsPath, "utf8");
  // Strip whole-line // comments. Comments in platform-settings.jsonc must stay on their own
  // lines so this does not corrupt values such as URLs that contain "//".
  const json = raw.replace(/^\s*\/\/.*$/gm, "");
  const settings = JSON.parse(json) as PlatformSettings;
  return { branding: settings.branding, socialLinks: settings.socialLinks };
}
