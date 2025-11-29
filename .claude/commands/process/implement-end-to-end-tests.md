---
description: Workflow for creating Playwright end-to-end tests for a [feature]
args:
  - name: featureId
    description: Feature ID to test (optional)
    required: false
---

# Implement End-to-End Tests Workflow

[FeatureId]: $ARGUMENTS

If a [FeatureId] is provided in the arguments above, read the [feature] to understand what needs testing. This workflow guides you through creating comprehensive end-to-end tests for the [feature]. It focuses on identifying what tests to write, planning complex scenarios, and ensuring tests follow the established conventions.

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

## Mandatory Preparation

**Note:**
- **Agentic mode**: The [featureId] comes from `current-task.json`. The CLI passes only the feature title as the slash command argument.
- **Standalone mode**: Feature details are passed as command arguments. If no [featureId] provided, ask user to describe what to test.

Read the feature from `[PRODUCT_MANAGEMENT_TOOL]` if provided.

## CRITICAL - Autonomous Operation

You run WITHOUT human supervision. NEVER ask for guidance or refuse to do work. Work with our team to find a solution.

**Token limits approaching?** Use `/compact` strategically (e.g., after being assigned a new task, but before reading task assignment, before catching up).

## Workflow

1. Understand the feature under test:
   - Study the frontend components and their interactions
   - Review API endpoints and authentication flows
   - Understand validation rules and error handling
   - Identify key user interactions and expected behaviors

2. Review existing test examples:
   - Read [End-to-End Tests](/.claude/rules/end-to-end-tests/end-to-end-tests.md) for detailed information
   - Examine [signup-flows.spec.ts](/application/account-management/WebApp/tests/e2e/signup-flows.spec.ts) and [login-flows.spec.ts](/application/account-management/WebApp/tests/e2e/login-flows.spec.ts) for inspiration
   - Note the structure, assertions, test organization, and the "Act & Assert:" comment format

3. Plan comprehensive test scenarios:
   - Identify standard user journeys through the feature
   - Plan for complex multi-session scenarios like:
     - Concurrent sessions: What happens when a user has two tabs open?
     - Cross-session state changes: What happens when state changes in one session affect another?
     - Authentication conflicts: How does the system handle authentication changes across sessions?
     - Form submissions across sessions: What happens with concurrent form submissions?
     - Antiforgery token handling: How are antiforgery tokens managed across tabs?
     - Browser navigation: Back/forward buttons, refresh, direct URL access
     - Network conditions: Slow connections, disconnections during operations
     - Input validation: Boundary values, special characters, extremely long inputs
     - Accessibility: Keyboard navigation, screen reader compatibility
     - Localization: Testing with different languages and formats

4. Categorize tests appropriately:
   - `@smoke`: Essential functionality that will run on deployment of any system
     - Create one comprehensive smoke.spec.ts per self-contained system
     - Test complete user journeys: signup → profile setup → invite users → manage roles → tenant settings → logout
     - Include validation errors, retries, and recovery scenarios within the journey
   - `@comprehensive`: More thorough tests covering edge cases that will run on deployment of the system under test
     - Focus on specific feature areas with deep testing of edge cases
     - Group related scenarios to minimize test count while maximizing coverage
   - `@slow`: Tests involving timeouts or waiting periods that will run ad-hoc, when features under test are changed

5. Create or update test structure:
   - For smoke tests: Create/update `application/[scs-name]/WebApp/tests/e2e/smoke.spec.ts`
   - For comprehensive tests: Create feature-specific files like `user-management-flows.spec.ts`, `role-management-flows.spec.ts`
   - Avoid creating many small, isolated tests—prefer comprehensive scenarios that test multiple aspects

6. **CRITICAL - Run watch tool to apply database migrations**:
   - Use **watch MCP tool** to restart server and run migrations
   - This MUST be done before running tests if backend schema changed
   - The tool starts .NET Aspire at https://localhost:9000

7. **CRITICAL - Run tests and verify they pass**:
   - Use **e2e MCP tool** to run your tests
   - Start with smoke tests: `e2e(smoke=true)`
   - Then run comprehensive tests with search terms: `e2e(searchTerms=["feature-name"])`
   - **ALL tests MUST pass** before proceeding
   - If tests fail: Fix them and run again (never proceed with failing tests)

8. Call reviewer to review your tests (skip in standalone mode):
   - Use Task tool to call `qa-reviewer` subagent
   - Provide paths to request and response files
   - Iterate with reviewer until approved

## Key Principles

- **Tests must pass**: Never complete without running tests and verifying they pass
- **Database migrations**: Always run watch tool if backend schema changed
- **Speed is critical**: Structure tests to minimize steps while maximizing coverage
- **Follow conventions**: Adhere to patterns in [End-to-End Tests](/.claude/rules/end-to-end-tests/end-to-end-tests.md)
- **Realistic user journeys**: Test scenarios that reflect actual user behavior
