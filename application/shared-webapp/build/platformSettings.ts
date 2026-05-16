import fs from "node:fs";
import path from "node:path";

export interface PlatformBranding {
  /** Product/platform name displayed throughout the application */
  productName: string;
  /** Support email address shown in the landing page footer and the in-app support dialog */
  supportEmail: string;
}

export interface PlatformSocialLinks {
  /** Public GitHub profile / repository URL */
  gitHub: string;
  /** Public LinkedIn profile URL */
  linkedIn: string;
  /** Public YouTube channel URL */
  youTube: string;
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
