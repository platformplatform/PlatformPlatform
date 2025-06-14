import { test } from '@playwright/test';

/**
 * Decorator function that wraps methods in Playwright test steps.
 * 
 * @param description - Required description for the test step
 * @returns Method decorator that wraps the original method in test.step()
 * 
 * @example
 * ```typescript
 * class MyPageObject {
 *   @step('Navigate to users page')
 *   async navigateToUsersPage() {
 *     await this.page.goto('/users');
 *   }
 * 
 *   @step('Invite user with email')
 *   async inviteUser(email: string) {
 *     await this.page.fill('[data-testid="email-input"]', email);
 *     await this.page.click('[data-testid="invite-button"]');
 *   }
 * }
 * ```
 */
export function step(description: string): any {
  // Support both decorator usage and direct function wrapping
  function stepFunction(targetOrFunction: any, propertyKey?: string, descriptor?: PropertyDescriptor): any {
    // If called with a function directly (not as decorator)
    if (typeof targetOrFunction === 'function' && !propertyKey) {
      const originalFunction = targetOrFunction;
      return function (this: any, ...args: any[]) {
        return test.step(description, async () => {
          const result = originalFunction.apply(this, args);
          // If the result is a Promise, await it; otherwise return directly
          return result && typeof result.then === 'function' ? await result : result;
        });
      };
    }

    // Decorator usage
    const target = targetOrFunction;
    
    // Get the descriptor if not provided
    if (!descriptor) {
      descriptor = Object.getOwnPropertyDescriptor(target, propertyKey!) || {
        value: target[propertyKey!],
        writable: true,
        enumerable: false,
        configurable: true
      };
    }

    const originalMethod = descriptor.value;

    if (!originalMethod || typeof originalMethod !== 'function') {
      throw new Error(`@step decorator can only be applied to methods. Property '${propertyKey}' is not a method.`);
    }

    descriptor.value = function (this: any, ...args: any[]) {
      return test.step(description, async () => {
        const result = originalMethod.apply(this, args);
        // If the result is a Promise, await it; otherwise return directly
        return result && typeof result.then === 'function' ? await result : result;
      });
    };

    // For legacy decorator support, define the property if needed
    if (!Object.getOwnPropertyDescriptor(target, propertyKey!)) {
      Object.defineProperty(target, propertyKey!, descriptor);
    }

    return descriptor;
  }

  return stepFunction;
}