---
description: Implement end-to-end tests for a [task] from a [feature] following the systematic workflow
---
# Implement End-to-End Tests Workflow

You are implementing: **{{{title}}}**

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

- **Agentic mode**: The [taskId] comes from `current-task.json`, not from command arguments. The CLI passes only the [taskTitle] as the slash command argument. You run autonomously without human supervision - work with your team to find solutions.
- **Standalone mode**: Task details are passed as command arguments `{{{title}}}`. If a [taskId] is provided, read [feature] and [task] from `[PRODUCT_MANAGEMENT_TOOL]`. If no [taskId] provided, ask user to describe what to test. There is no `current-task.json`.

## STEP 0: Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.agent/reference/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
   - `requestFilePath`: Request file path
   - `featureId`: [FeatureId] (the feature to test, or "ad-hoc" for ad-hoc work)
   - `taskId`: [TaskId] (the task you're implementing, or "ad-hoc-yyyyMMdd-HHmm" for ad-hoc work)
   - `taskTitle`: Task title

   **If current-task.json does NOT exist:**

   This means there is no active task assignment. Call CompleteWork immediately to terminate your session:

   ```
   Call CompleteWork with:
   - mode: "task"
   - agentType: your agent type
   - taskSummary: "No active task assignment found"
   - responseContent: "Session invoked without active task. Current-task.json does not exist. Terminating session."
   - feedback: "[system] Session was invoked with /process:implement-end-to-end-tests but no current-task.json exists - possible double invocation after completion"
   ```

   DO NOT proceed with any other work. DO NOT just say "nothing to do". Call CompleteWork immediately to terminate the session.

3. **Read the request file** from the path in `requestFilePath`.

4. **Read [feature] from [PRODUCT_MANAGEMENT_TOOL]** if `featureId` is NOT "ad-hoc" to understand what needs testing.

5. **Create Todo List**

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Read [task] from [PRODUCT_MANAGEMENT_TOOL] and update status to [Active]", "status": "pending", "activeForm": "Reading task and updating status to Active"},
    {"content": "Understand the feature under test", "status": "pending", "activeForm": "Understanding feature under test"},
    {"content": "Research existing patterns for this [task] type", "status": "pending", "activeForm": "Researching existing patterns"},
    {"content": "Plan test scenarios", "status": "pending", "activeForm": "Planning test scenarios"},
    {"content": "Categorize tests appropriately", "status": "pending", "activeForm": "Categorizing tests"},
    {"content": "Create or update test structure", "status": "pending", "activeForm": "Creating or updating test structure"},
    {"content": "Run tests and verify they pass", "status": "pending", "activeForm": "Running and verifying tests"},
    {"content": "Delegate to reviewer subagent (skip in standalone mode)", "status": "pending", "activeForm": "Delegating to reviewer"},
    {"content": "MANDATORY: Call CompleteWork after reviewer approval (skip in standalone mode)", "status": "pending", "activeForm": "Calling CompleteWork"}
  ]
}
```

---

## Workflow Steps

**STEP 1**: Read [task] from [PRODUCT_MANAGEMENT_TOOL] and update status to [Active]

**If `featureId` is NOT "ad-hoc" (regular task from a feature):**
1. Read [feature] from `featureId` in [PRODUCT_MANAGEMENT_TOOL] to understand the full PRD context
2. Read [task] from `taskId` in [PRODUCT_MANAGEMENT_TOOL] to get task details and test requirements
3. **Update [task] status to [Active]** in `[PRODUCT_MANAGEMENT_TOOL]`
4. **If [task] lookup fails** (not found, already completed, or error): This is a coordination error. Report a problem and call CompleteWork explaining the task could not be found.

**If `featureId` is "ad-hoc" (ad-hoc work):**
- Skip [PRODUCT_MANAGEMENT_TOOL] operations
- Still follow full engineer → reviewer → commit cycle

**STEP 2**: Understand the feature under test

- Study the frontend components and their interactions
- Review API endpoints and authentication flows
- Understand validation rules and error handling
- Identify key user interactions and expected behaviors

**STEP 3**: Research existing patterns for this [task] type

Research the codebase to find similar E2E test implementations. Look for existing tests that handle similar features, user flows, or test patterns that can guide your implementation.

- Search for similar test files in `application/*/WebApp/tests/e2e/`
- Review test patterns: fixture usage, page object patterns, assertion styles
- Note test categorization (@smoke, @comprehensive, @slow) used in similar features
- Look for reusable test utilities and helper functions

**STEP 4**: Plan test scenarios

**Speed is essential**: Tests must run fast. Prefer extending existing tests over creating new ones. Design tests that validate multiple scenarios in a single test run.

**Planning approach**:
- **First, check existing tests**: Can you extend an existing test file instead of creating a new one?
- **Combine scenarios**: Design tests that validate multiple aspects in one user journey (e.g., signup → profile update → settings change in one test)
- **Identify essential user journeys**: Focus on the most important paths users will take
- **Consider edge cases within the journey**: Don't create separate tests for edge cases - integrate them into the main journey where possible

**Scenarios to consider (integrate into efficient tests)**:
- Standard user journeys (signup, login, CRUD operations)
- Validation errors and recovery (test within the main journey, not separately)
- Browser navigation (back/forward, refresh) if critical to the feature
- Multi-session scenarios ONLY if the feature specifically involves multiple sessions
- Input validation (boundary values, special characters) within normal test flow

**STEP 5**: Categorize tests appropriately

- `@smoke`: Essential functionality that will run on deployment of any system
  - Create one comprehensive smoke.spec.ts per self-contained system
  - Test complete user journeys: signup → profile setup → invite users → manage roles → tenant settings → logout
  - Include validation errors, retries, and recovery scenarios within the journey
- `@comprehensive`: More thorough tests covering edge cases that will run on deployment of the system under test
  - Focus on specific feature areas with deep testing of edge cases
  - Group related scenarios to minimize test count while maximizing coverage
- `@slow`: Tests involving timeouts or waiting periods that will run ad-hoc, when features under test are changed

**STEP 6**: Create or update test structure

- For smoke tests: Create/update `application/[scs-name]/WebApp/tests/e2e/smoke.spec.ts`
- For comprehensive tests: Create feature-specific files like `user-management-flows.spec.ts`, `role-management-flows.spec.ts`
- Avoid creating many small, isolated tests—prefer comprehensive scenarios that test multiple aspects

**STEP 7**: Run tests and verify they pass

- Use **end-to-end MCP tool** to run your tests
- Start with smoke tests: `end-to-end(smoke=true)`
- Then run comprehensive tests with search terms: `end-to-end(searchTerms=["feature-name"])`
- All tests must pass before proceeding
- If tests fail: Fix them and run again (don't proceed with failing tests)

**If tests fail with backend errors or suspect server issues**:
- Use **run MCP tool** to restart server and run database migrations
- The tool starts .NET Aspire at https://localhost:9000
- Re-run tests after server restart

**STEP 8**: Delegate to reviewer subagent (skip in standalone mode)

**Before calling reviewer (every time, including re-reviews)**:

**1. Update [task] status to [Review]** in [PRODUCT_MANAGEMENT_TOOL] (if featureId is NOT "ad-hoc"):
   - This applies to every review request, not just the first one.
   - When reviewer rejects and moves status to [Active], move it back to [Review] when requesting re-review.
   - Skip this only for ad-hoc work (featureId is "ad-hoc").

**2. Zero tolerance verification**: Confirm all tests pass with zero failures. Don't request review with failing tests.

**3. Identify your changed files**:
- Run `git status --porcelain` to see ALL changed files.
- List YOUR files (test files you created/modified) in "Files Changed" section (one per line with status).

Delegate to reviewer subagent:

**Delegation format**:
```
[One short sentence: what tests you created]

## Files Changed
- path/to/test1.spec.ts
- path/to/test2.spec.ts

Request: {requestFilePath}
Response: {responseFilePath}
```

**MCP call parameters**:
- `senderAgentType`: qa-engineer
- `targetAgentType`: qa-reviewer
- `taskTitle`: From current-task.json
- `markdownContent`: Your delegation message above
- `branch`: From current-task.json
- `featureId`: From current-task.json
- `taskId`: From current-task.json
- `resetMemory`: false
- `requestFilePath`: From current-task.json
- `responseFilePath`: From current-task.json

**Review loop**:
- If reviewer returns NOT APPROVED → Fix issues → Update [task] status to [Review] → Call reviewer subagent again.
- If reviewer returns APPROVED → Check your files are committed → Proceed to completion.
- Don't call CompleteWork unless reviewer approved and committed your code.
- Don't commit code yourself - only the reviewer commits.
- If rejected 3+ times with same feedback despite all tests passing: Report problem with severity: error, then stop. Don't call CompleteWork, don't proceed with work - the user will take over manually.

**STEP 9**: Call CompleteWork after reviewer approval (skip in standalone mode)

After completing all work and receiving reviewer approval, call the MCP **CompleteWork** tool with `mode: "task"` to signal completion. This tool call will terminate your session.

CompleteWork requires reviewer approval and committed code.

**Before calling CompleteWork**:
1. Ensure all work is complete and all todos are marked as completed.
2. Write a comprehensive response (what you accomplished, notes for Coordinator).
3. Create an objective technical summary in sentence case (like a commit message).
4. Reflect on your experience and write categorized feedback using prefixes:
   - `[system]` - Workflow, MCP tools, agent coordination, message handling.
   - `[requirements]` - Requirements clarity, acceptance criteria, test coverage needs.
   - `[code]` - Test patterns, E2E conventions, test organization guidance.

   Examples:
   - `[system] CompleteWork returned errors until title was less than 100 characters - consider adding format description`.
   - `[requirements] Test description mentioned "admin user" but unclear if TenantAdmin or WorkspaceAdmin`.
   - `[code] No existing examples found for testing multi-session scenarios in this context`.

   You can provide multiple categorized items. Use report_problem for urgent system bugs during work.

**Call MCP CompleteWork tool**:
- `mode`: "task"
- `agentType`: qa-engineer
- `taskSummary`: Objective technical description of what was implemented (imperative mood, sentence case). Examples: "Add E2E tests for user role management", "Implement smoke tests for tenant settings", "Fix flaky tests in authentication flow". NEVER use subjective evaluations like "Excellent tests" or "Clean code".
- `responseContent`: Your full response in markdown
- `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes as described above

⚠️ Your session terminates IMMEDIATELY after calling CompleteWork

---

## Key Principles

- **Tests must pass**: Never complete without running tests and verifying they pass
- **Database migrations**: Always run the run tool if backend schema changed
- **Speed is critical**: Structure tests to minimize steps while maximizing coverage
- **Follow conventions**: Adhere to patterns in [End-to-End Tests](/.agent/rules/end-to-end-tests/end-to-end-tests.md)
- **Realistic user journeys**: Test scenarios that reflect actual user behavior
