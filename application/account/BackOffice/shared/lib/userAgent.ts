import { t } from "@lingui/core/macro";

// Order matters: more specific patterns first. Modern Chromium-based browsers like Opera and Edge
// also include `Chrome/` in their user-agent string, so the `OPR/` and `Edg/` matches must run
// before `Chrome/`. Firefox occasionally announces a Safari token for compatibility, and every
// Android browser identifies itself as Linux too — keep the platform-specific markers above the
// generic ones so detection lands on the most specific match.
const browserPatterns: Array<{ pattern: RegExp; name: string }> = [
  { pattern: /OPR\/[\d.]+/, name: "Opera" },
  { pattern: /Edg\/[\d.]+/, name: "Edge" },
  { pattern: /Firefox\/[\d.]+/, name: "Firefox" },
  { pattern: /Chrome\/[\d.]+/, name: "Chrome" },
  { pattern: /Safari\/[\d.]+/, name: "Safari" }
];

const osPatterns: Array<{ pattern: RegExp; name: string }> = [
  { pattern: /Android/, name: "Android" },
  { pattern: /iPhone|iPad/, name: "iOS" },
  { pattern: /Windows NT/, name: "Windows" },
  { pattern: /Mac OS X/, name: "macOS" },
  { pattern: /Linux/, name: "Linux" }
];

export function parseUserAgent(userAgent: string): { browser: string; os: string } {
  const unknown = t`Unknown`;
  let browser = unknown;
  let os = unknown;

  for (const { pattern, name } of browserPatterns) {
    if (pattern.test(userAgent)) {
      browser = name;
      break;
    }
  }

  for (const { pattern, name } of osPatterns) {
    if (pattern.test(userAgent)) {
      os = name;
      break;
    }
  }

  return { browser, os };
}
