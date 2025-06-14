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
          const startTime = performance.now();
          const result = originalFunction.apply(this, args);
          const finalResult = result && typeof result.then === 'function' ? await result : result;
          const endTime = performance.now();
          const duration = endTime - startTime;

          // Debug timing output if enabled
          if (process.env.PLAYWRIGHT_SHOW_DEBUG_TIMING === 'true') {
            const timestamp = new Date().toLocaleTimeString('en-US', { 
              hour12: false, 
              hour: '2-digit', 
              minute: '2-digit', 
              second: '2-digit',
              fractionalSecondDigits: 3 
            });
            const durationSeconds = (duration / 1000).toFixed(3);
            
            // Color coding: green (<250ms), yellow (250ms-1s), red (>1s)
            let colorCode = '\x1b[32m'; // Green
            if (duration >= 1000) {
              colorCode = '\x1b[31m'; // Red
            } else if (duration >= 250) {
              colorCode = '\x1b[33m'; // Yellow
            }
            const resetCode = '\x1b[0m';
            
            console.log(`${timestamp} - ${colorCode}[${durationSeconds}s]${resetCode} - ${description}`);
          }
          
          return finalResult;
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
        const startTime = performance.now();
        const result = originalMethod.apply(this, args);
        const finalResult = result && typeof result.then === 'function' ? await result : result;
        const endTime = performance.now();
        const duration = endTime - startTime;

        // Debug timing output if enabled
        if (process.env.PLAYWRIGHT_SHOW_DEBUG_TIMING === 'true') {
          const timestamp = new Date().toLocaleTimeString('en-US', { 
            hour12: false, 
            hour: '2-digit', 
            minute: '2-digit', 
            second: '2-digit',
            fractionalSecondDigits: 3 
          });
          const durationSeconds = (duration / 1000).toFixed(3);
          
          // Color coding: green (<250ms), yellow (250ms-1s), red (>1s)
          let colorCode = '\x1b[32m'; // Green
          if (duration >= 1000) {
            colorCode = '\x1b[31m'; // Red
          } else if (duration >= 250) {
            colorCode = '\x1b[33m'; // Yellow
          }
          const resetCode = '\x1b[0m';
          
          console.log(`${timestamp} - ${colorCode}[${durationSeconds}s]${resetCode} - ${description}`);
        }
        
        return finalResult;
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