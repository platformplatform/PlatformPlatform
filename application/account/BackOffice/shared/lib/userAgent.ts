import { t } from "@lingui/core/macro";

const browserPatterns: Array<{ pattern: RegExp; name: string }> = [
  { pattern: /Edg\/[\d.]+/, name: "Edge" },
  { pattern: /Chrome\/[\d.]+/, name: "Chrome" },
  { pattern: /Firefox\/[\d.]+/, name: "Firefox" },
  { pattern: /Safari\/[\d.]+/, name: "Safari" },
  { pattern: /OPR\/[\d.]+/, name: "Opera" }
];

const osPatterns: Array<{ pattern: RegExp; name: string }> = [
  { pattern: /Windows NT 10/, name: "Windows" },
  { pattern: /Windows NT/, name: "Windows" },
  { pattern: /Mac OS X/, name: "macOS" },
  { pattern: /Linux/, name: "Linux" },
  { pattern: /Android/, name: "Android" },
  { pattern: /iPhone|iPad/, name: "iOS" }
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
