import { promises as fs } from "node:fs";
import path from "node:path";
import type { BrowserContext, Page } from "@playwright/test";
import type { StorageStateInfo } from "../types/auth";

/**
 * Save authentication state from a page's context to a file
 * Only captures cookies as session storage is not needed for PlatformPlatform's token architecture
 */
export async function saveAuthenticationState(page: Page, filePath: string): Promise<void> {
  // Ensure directory exists
  await ensureDirectoryExists(path.dirname(filePath));

  // Save storage state (cookies and localStorage)
  await page.context().storageState({ path: filePath });
}

/**
 * Load authentication state from a file into a browser context
 */
export async function loadAuthenticationState(_context: BrowserContext, filePath: string): Promise<void> {
  // Storage state is loaded when creating the context, not after
  // This function is mainly for validation and future use
  const exists = await fileExists(filePath);
  if (!exists) {
    throw new Error(`Authentication state file not found: ${filePath}`);
  }
}

/**
 * Get the storage state file path for a specific worker, role, and system
 */
export function getStorageStatePath(workerIndex: number, userRole: string, selfContainedSystemPrefix?: string): string {
  const baseDir = path.join(process.cwd(), ".auth");
  const systemPrefix = selfContainedSystemPrefix || "default";
  return path.join(baseDir, systemPrefix, `worker-${workerIndex}-${userRole.toLowerCase()}.json`);
}

/**
 * Check if an authentication state file is valid and exists
 */
export async function isAuthenticationStateValid(filePath: string): Promise<boolean> {
  try {
    const exists = await fileExists(filePath);
    if (!exists) {
      return false;
    }

    // Check if file is not empty and contains valid JSON
    const content = await fs.readFile(filePath, "utf-8");
    const state = JSON.parse(content);

    // Basic validation - should have cookies or origins
    return state && (state.cookies || state.origins);
  } catch {
    return false;
  }
}

/**
 * Ensure the .auth directory structure exists and is properly configured
 */
export async function ensureAuthDirectorySetup(): Promise<void> {
  const authDir = path.join(process.cwd(), ".auth");
  await ensureDirectoryExists(authDir);

  // Create system-specific directories as needed
  const defaultDir = path.join(authDir, "default");
  await ensureDirectoryExists(defaultDir);
}

/**
 * Get information about a storage state file
 */
export async function getStorageStateInfo(filePath: string): Promise<StorageStateInfo> {
  const isValid = await isAuthenticationStateValid(filePath);
  let lastModified: Date | undefined;

  if (isValid) {
    try {
      const stats = await fs.stat(filePath);
      lastModified = stats.mtime;
    } catch {
      // Ignore stat errors
    }
  }

  return {
    filePath,
    isValid,
    lastModified
  };
}

/**
 * Clean up invalid or expired authentication state files
 */
export async function cleanupInvalidAuthenticationStates(directory: string): Promise<void> {
  try {
    const files = await fs.readdir(directory);
    const jsonFiles = files.filter((file) => file.endsWith(".json"));

    for (const file of jsonFiles) {
      const filePath = path.join(directory, file);
      const isValid = await isAuthenticationStateValid(filePath);

      if (!isValid) {
        await fs.unlink(filePath);
      }
    }
  } catch {
    // Ignore cleanup errors - directory might not exist
  }
}

// Helper functions

async function ensureDirectoryExists(dirPath: string): Promise<void> {
  try {
    await fs.mkdir(dirPath, { recursive: true });
  } catch (error: unknown) {
    if (error instanceof Error && "code" in error && error.code !== "EEXIST") {
      throw error;
    }
  }
}

async function fileExists(filePath: string): Promise<boolean> {
  try {
    await fs.access(filePath);
    return true;
  } catch {
    return false;
  }
}
