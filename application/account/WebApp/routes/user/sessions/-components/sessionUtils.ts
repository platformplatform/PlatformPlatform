import { t } from "@lingui/core/macro";
import { MonitorIcon, SmartphoneIcon, TabletIcon } from "lucide-react";

import { type components, DeviceType, LoginMethod } from "@/shared/lib/api/client";

export type UserSessionInfo = components["schemas"]["UserSessionInfo"];

export function parseUserAgent(userAgent: string): { browser: string; os: string } {
  const browserPatterns = [
    { pattern: /Edg\/[\d.]+/, name: "Edge" },
    { pattern: /Chrome\/[\d.]+/, name: "Chrome" },
    { pattern: /Firefox\/[\d.]+/, name: "Firefox" },
    { pattern: /Safari\/[\d.]+/, name: "Safari" },
    { pattern: /OPR\/[\d.]+/, name: "Opera" }
  ];

  const osPatterns = [
    { pattern: /Windows NT 10/, name: "Windows" },
    { pattern: /Windows NT/, name: "Windows" },
    { pattern: /Mac OS X/, name: "macOS" },
    { pattern: /Linux/, name: "Linux" },
    { pattern: /Android/, name: "Android" },
    { pattern: /iPhone|iPad/, name: "iOS" }
  ];

  let browser = t`Unknown`;
  let os = t`Unknown`;

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

export function getDeviceTypeLabel(deviceType: UserSessionInfo["deviceType"]): string {
  switch (deviceType) {
    case DeviceType.Mobile:
      return t`Mobile`;
    case DeviceType.Tablet:
      return t`Tablet`;
    case DeviceType.Desktop:
      return t`Desktop`;
    default:
      return t`Unknown`;
  }
}

export function getDeviceIcon(deviceType: UserSessionInfo["deviceType"]) {
  switch (deviceType) {
    case DeviceType.Mobile:
      return SmartphoneIcon;
    case DeviceType.Tablet:
      return TabletIcon;
    case DeviceType.Desktop:
      return MonitorIcon;
    default:
      return MonitorIcon;
  }
}

export function getLoginMethodLabel(loginMethod: UserSessionInfo["loginMethod"]): string {
  switch (loginMethod) {
    case LoginMethod.OneTimePassword:
      return t`One-time password`;
    case LoginMethod.Google:
      return t`Google`;
    default:
      return t`Unknown`;
  }
}
