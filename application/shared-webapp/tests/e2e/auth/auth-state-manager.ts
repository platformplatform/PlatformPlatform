import { promises as fs } from "node:fs";
import type { BrowserContext, Page } from "@playwright/test";
import type { UserRole } from "../types/auth";
import {
  getStorageStatePath,
  isAuthenticationStateValid,
  loadAuthenticationState,
  saveAuthenticationState
} from "./storage-state";

/**
 * Authentication state manager for handling persistence and validation
 */
export class AuthStateManager {
  private workerIndex: number;
  private selfContainedSystemPrefix?: string;

  constructor(workerIndex: number, selfContainedSystemPrefix?: string) {
    this.workerIndex = workerIndex;
    this.selfContainedSystemPrefix = selfContainedSystemPrefix;
  }

  /**
   * Get the storage state file path for a specific role
   * @param role User role
   * @returns Path to the storage state file
   */
  getStateFilePath(role: UserRole): string {
    return getStorageStatePath(this.workerIndex, role.toLowerCase(), this.selfContainedSystemPrefix);
  }

  /**
   * Check if authentication state exists and is valid for a role
   * @param role User role
   * @returns Promise resolving to true if auth state is valid
   */
  async hasValidAuthState(role: UserRole): Promise<boolean> {
    const filePath = this.getStateFilePath(role);
    return await isAuthenticationStateValid(filePath);
  }

  /**
   * Load authentication state for a role into a browser context
   * @param context Browser context
   * @param role User role
   * @returns Promise resolving when state is loaded
   */
  async loadAuthState(context: BrowserContext, role: UserRole): Promise<void> {
    const filePath = this.getStateFilePath(role);
    await loadAuthenticationState(context, filePath);
  }

  /**
   * Save authentication state for a role from a page
   * @param page Playwright page
   * @param role User role
   * @returns Promise resolving when state is saved
   */
  async saveAuthState(page: Page, role: UserRole): Promise<void> {
    const filePath = this.getStateFilePath(role);
    await saveAuthenticationState(page, filePath);
  }

  /**
   * Test if the authentication state is still valid by accessing a protected endpoint
   * @param page Playwright page with loaded auth state
   * @returns Promise resolving to true if auth is still valid
   */
  async validateAuthState(page: Page): Promise<boolean> {
    try {
      // Navigate to admin dashboard which requires authentication
      await page.goto("/admin");

      // If we get redirected to login, auth is invalid
      if (page.url().includes("/login")) {
        return false;
      }

      // Check for the presence of authenticated content
      // This is a basic check - in the future we might want to test specific API endpoints
      const hasAuthenticatedContent =
        (await page
          .locator('[data-testid="authenticated-content"], .admin-dashboard, h1:has-text("Welcome")')
          .count()) > 0;

      return hasAuthenticatedContent;
    } catch {
      // If any error occurs during validation, consider auth invalid
      return false;
    }
  }

  /**
   * Clear authentication state for a specific role
   * @param role User role
   * @returns Promise resolving when state is cleared
   */
  async clearAuthState(role: UserRole): Promise<void> {
    const filePath = this.getStateFilePath(role);
    try {
      await fs.unlink(filePath);
    } catch {
      // File might not exist, which is fine
    }
  }

  /**
   * Clear all authentication states for this worker
   * @returns Promise resolving when all states are cleared
   */
  async clearAllAuthStates(): Promise<void> {
    const roles: UserRole[] = ["Owner", "Admin", "Member"];
    await Promise.all(roles.map((role) => this.clearAuthState(role)));
  }

  /**
   * Get a summary of authentication state validity for all roles
   * @returns Promise resolving to an object with validity status per role
   */
  async getAuthStateSummary(): Promise<Record<UserRole, boolean>> {
    const roles: UserRole[] = ["Owner", "Admin", "Member"];
    const results = await Promise.all(
      roles.map(async (role) => ({
        role,
        isValid: await this.hasValidAuthState(role)
      }))
    );

    return results.reduce(
      (summary, { role, isValid }) => {
        summary[role] = isValid;
        return summary;
      },
      {} as Record<UserRole, boolean>
    );
  }
}

/**
 * Create an AuthStateManager instance for the current worker
 * @param workerIndex Playwright worker index
 * @param selfContainedSystemPrefix Optional system prefix
 * @returns AuthStateManager instance
 */
export function createAuthStateManager(workerIndex: number, selfContainedSystemPrefix?: string): AuthStateManager {
  return new AuthStateManager(workerIndex, selfContainedSystemPrefix);
}
