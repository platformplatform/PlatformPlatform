/// <reference types="node" />

/**
 * Shared constants for End2End tests
 */

import { existsSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";

function readBasePort(): number {
  // Walk up from this file (application/shared-webapp/tests/e2e/utils) to the repo root and read
  // .workspace/port.txt. E2E tests must have an explicit port file — silent defaulting would mask
  // setup problems in deterministic test environments.
  const repoRoot = resolve(__dirname, "..", "..", "..", "..", "..");
  const portFile = join(repoRoot, ".workspace", "port.txt");
  if (!existsSync(portFile)) {
    throw new Error(
      "E2E tests require .workspace/port.txt to exist. Start Aspire AppHost first to bootstrap it."
    );
  }
  const content = readFileSync(portFile, "utf8").trim();
  const parsed = Number.parseInt(content, 10);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    throw new Error(`.workspace/port.txt must contain a positive integer port number. Got: '${content}'`);
  }
  return parsed;
}

const BASE_PORT = readBasePort();
const DEFAULT_BASE_URL = `https://app.dev.localhost:${BASE_PORT}`;

function readProductName(): string {
  // E2E tests must read the brand name from the same JSONC the backend and frontend build use,
  // so a downstream rebrand only needs to edit platform-settings.jsonc (no test churn).
  const repositoryRoot = resolve(__dirname, "..", "..", "..", "..", "..");
  const settingsPath = join(repositoryRoot, "application", "platform-settings.jsonc");
  const rawSettings = readFileSync(settingsPath, "utf8");
  // Strip whole-line // comments (platform-settings.jsonc guarantees comments stay on their own
  // lines so this does not corrupt values such as URLs that contain "//").
  const parseableSettings = rawSettings.replace(/^\s*\/\/.*$/gm, "");
  const settings = JSON.parse(parseableSettings) as { branding: { productName: string } };
  return settings.branding.productName;
}

export const productName = readProductName();
// Back-office runs on a dedicated Kestrel listener at BASE_PORT + 1 (PortAllocation.BackOfficeApi).
// AppGateway only routes the user-facing host post host-isolation refactor.
const DEFAULT_BACK_OFFICE_BASE_URL = `https://back-office.dev.localhost:${BASE_PORT + 1}`;

export const isWindows = process.platform === "win32";
export const isLinux = process.platform === "linux";

/**
 * Get the base URL for tests
 */
export function getBaseUrl(): string {
  return process.env.PUBLIC_URL ?? DEFAULT_BASE_URL;
}

/**
 * Get the back-office base URL for tests. Back-office is hosted on its own Kestrel
 * listener (BASE_PORT + 1) — AppGateway is not in the path, mirroring the Azure
 * post-split topology.
 */
export function getBackOfficeBaseUrl(): string {
  return process.env.BACK_OFFICE_PUBLIC_URL ?? DEFAULT_BACK_OFFICE_BASE_URL;
}

/**
 * Check if we're running against localhost
 */
export function isLocalhost(): boolean {
  return getBaseUrl() === DEFAULT_BASE_URL;
}
