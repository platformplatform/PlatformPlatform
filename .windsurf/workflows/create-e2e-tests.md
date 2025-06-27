---
description: Workflow for creating end-to-end tests
---

# E2E Testing Workflow

This workflow guides you through the process of creating comprehensive end-to-end tests for specific features like login and signup. It focuses on identifying what tests to write, planning complex scenarios, and ensuring tests follow the established conventions.

## Workflow

1. Understand the feature under test:
   - Study the frontend components and their interactions.
   - Review API endpoints and authentication flows.
   - Understand validation rules and error handling.
   - Identify key user interactions and expected behaviors.

2. Use Browser MCP to explore the webapp functionality:
   - Navigate to the application: `mcp0_browser_navigate({ url: "https://localhost:9000" })`.
   - Interact with the feature manually to understand user flows.
   - Take snapshots to identify UI elements and their structure.
   - Document key interactions and expected behaviors.
   - Note any edge cases or potential issues discovered during exploration.

3. Review existing test examples:
   - Read [End-to-End Tests](/.windsurf/rules/end-to-end-tests/e2e-tests.md) for detailed information.
   - Examine [signup.spec.ts](/application/account-management/WebApp/tests/e2e/signup.spec.ts) and [login.spec.ts](/application/account-management/WebApp/tests/e2e/login.spec.ts) for inspiration.
   - Note the structure, assertions, test organization, and the "Act & Assert:" comment format.

4. Plan comprehensive test scenarios:
   - Identify standard user journeys through the feature.
   - Plan for complex multi-session scenarios like:
     - Concurrent sessions: What happens when a user has two tabs open?
     - Cross-session state changes: What happens when state changes in one session affect another?
     - Authentication conflicts: How does the system handle authentication changes across sessions?
     - Form submissions across sessions: What happens with concurrent form submissions?
     - Antiforgery token handling: How are antiforgery tokens managed across tabs?
     - Browser navigation: Back/forward buttons, refresh, direct URL access.
     - Network conditions: Slow connections, disconnections during operations.
     - Input validation: Boundary values, special characters, extremely long inputs.
     - Accessibility: Keyboard navigation, screen reader compatibility.
     - Localization: Testing with different languages and formats.

5. Categorize tests appropriately:
   - `@smoke`: Essential functionality that will run on deployment of any system.
     - Create one comprehensive smoke.spec.ts per self-contained system.
     - Test complete user journeys: signup → profile setup → invite users → manage roles → tenant settings → logout.
     - Include validation errors, retries, and recovery scenarios within the journey.
   - `@comprehensive`: More thorough tests covering edge cases that will run on deployment of the system under test.
     - Focus on specific feature areas with deep testing of edge cases.
     - Group related scenarios to minimize test count while maximizing coverage.
   - `@slow`: Tests involving timeouts or waiting periods that will run ad-hoc, when features under test are changed.

6. Create or update test structure:
   - For smoke tests: Create/update `application/[scs-name]/WebApp/tests/e2e/smoke.spec.ts`.
   - For comprehensive tests: Create feature-specific files like `user-management.spec.ts`, `authentication.spec.ts`.
   - Avoid creating many small, isolated tests - prefer comprehensive scenarios that test multiple aspects.

## Key principles

- Comprehensive coverage: Test all critical paths and important edge cases.
- Follow conventions: Adhere to the established patterns in [End-to-End Tests](/.windsurf/rules/end-to-end-tests/e2e-tests.md).
- Clear organization: Properly categorize tests and use descriptive names.
- Realistic user journeys: Test scenarios that reflect actual user behavior.
