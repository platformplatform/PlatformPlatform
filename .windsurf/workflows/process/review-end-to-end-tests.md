---
description: Review end-to-end test implementation for a story task
auto_execution_mode: 3
---

# Review E2E Tests Workflow

You are reviewing: **{{{title}}}**

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

## Mandatory Preparation

**Note:**
- **Agentic mode**: The review request comes from `current-task.json`. The CLI passes only the task title as the slash command argument.
- **Standalone mode**: Test files are passed as command arguments `{{{title}}}`. Read test files from user-provided paths or from `git status`.

**Read your `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{your-agent-type}/current-task.json`** to get:
- `requestFilePath`: Request file path
- `taskId`: [TaskId] (if applicable)
- `title`: Task title

**Then read the request and response files** to understand what was implemented.

---

## Create Todo List

```json
{
  "todos": [
    {"content": "Read context and engineer's implementation", "status": "pending", "activeForm": "Reading context"},
    {"content": "Run watch tool to apply migrations", "status": "pending", "activeForm": "Running watch tool"},
    {"content": "Run e2e tests and verify ALL pass", "status": "pending", "activeForm": "Running E2E tests"},
    {"content": "Review E2E test file structure and organization", "status": "pending", "activeForm": "Reviewing test structure"},
    {"content": "Verify test categorization (@smoke vs @comprehensive)", "status": "pending", "activeForm": "Verifying categorization"},
    {"content": "Check all steps follow 'Do something & verify result' pattern", "status": "pending", "activeForm": "Checking step patterns"},
    {"content": "Reject if any sleep statements found", "status": "pending", "activeForm": "Checking for sleep statements"},
    {"content": "Verify tests leverage existing fixtures and helpers", "status": "pending", "activeForm": "Verifying test efficiency"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing if approved"},
    {"content": "Report any workflow errors encountered (wrong paths, missing tools, etc.)", "status": "pending", "activeForm": "Reporting workflow errors"}
  ]
}
```

---

## Workflow Steps

**STEP 1**: Read all context.

**STEP 2**: Run watch and E2E tools:
- Use **watch MCP tool** to restart server and run migrations
- Use **e2e MCP tool** to run tests: `e2e(searchTerms=["feature-name"])`
- **ALL tests MUST pass with ZERO failures to approve**
- **Verify ZERO console errors** during test execution
- **Verify ZERO network errors** (no unexpected 4xx/5xx responses)
- If ANY test fails: REJECT
- If ANY console errors: REJECT
- If ANY network errors: REJECT

**STEP 3**: Study E2E rules:
- Read [End-to-End Tests](/.windsurf/rules/end-to-end-tests/end-to-end-tests.md)
- Ensure engineer followed all patterns

**STEP 4**: Review test file structure.

**Critical Check 1 - Test Count:**
- Normally ONE new `@comprehensive` test per feature
- Existing `@smoke` tests should be updated, not duplicated
- For BIG features: Allow both new `@smoke` and new `@comprehensive`
- **REJECT if too many small test files created**

**STEP 5**: Review each test step.

**Critical Check 2 - Step Naming Pattern:**
- **EVERY step MUST follow**: "Do something & verify result"
- ✅ Good: `"Submit login form & verify authentication"`
- ❌ Bad: `"Verify button is visible"` (no action)
- ❌ Bad: `"Test login"` (uses "test" prefix)
- **REJECT if steps don't follow pattern**

**Critical Check 3 - No Sleep Statements:**
- Search for: `waitForTimeout`, `sleep`, `delay`, `setTimeout`
- **REJECT immediately if found—no exceptions, no discussion**
- Playwright auto-waits—sleep is NEVER needed in any scenario
- Demand Playwright await assertions instead:
  - Use `toBeVisible()`, `toHaveURL()`, `toContainText()`, etc.
  - These built-in auto-wait mechanisms handle all timing scenarios

**STEP 6**: Review test efficiency.

**Critical Check 4 - Leverage Existing Logic:**
- Verify tests use fixtures: `{ page }`, `{ ownerPage }`, `{ adminPage }`, `{ memberPage }`
- Verify tests use helpers: `expectToastMessage`, `expectValidationError`, etc.
- **REJECT if tests duplicate existing logic**

**Critical Check 5 - Speed Optimization:**
- Tests should test MANY things in FEW steps
- Avoid excessive navigation or setup
- Group related scenarios together
- **REJECT if tests are unnecessarily slow**

**STEP 7**: Decide - APPROVED or NOT APPROVED.

**Aim for perfection, not "good enough".**

**APPROVED only if ALL criteria met:**
- ✓ All E2E tests passed with zero failures
- ✓ Zero console errors during test execution
- ✓ Zero network errors during test execution
- ✓ No sleep statements found
- ✓ All steps follow "Do something & verify result" pattern
- ✓ Tests use existing fixtures and helpers
- ✓ Tests are efficient and fast

**REJECT if ANY issue exists—no exceptions. Common rationalizations to AVOID:**
- ✗ "Test failed but feature works manually" → REJECT, fix test
- ✗ "Console error unrelated to E2E code" → REJECT ANYWAY
- ✗ "It's just a warning" → REJECT, zero means ZERO
- ✗ "Previous test run passed" → REJECT ANYWAY if current run has issues

**When rejecting:** Do full review first, then reject with ALL issues listed (avoid multiple rounds).

**STEP 8**: If APPROVED, commit changes:
1. Stage test files: `git add <file>` for each test file
2. Commit: One line, imperative form, no description, no co-author
3. Get hash: `git rev-parse HEAD`

🚨 **NEVER use `git add -A` or `git add .`**

**STEP 9**: Signal completion (skip in standalone mode)

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

**Call MCP CompleteWork tool**:
- `mode`: "review"
- `agentType`: qa-reviewer
- `commitHash`: Commit hash if approved, null/empty if rejected
- `rejectReason`: Rejection reason if rejected, null/empty if approved
- `responseContent`: Your full review feedback
- `feedback`: Mandatory categorized feedback using prefixes:
  - `[system]` — Workflow, MCP tools, agent coordination, message handling
  - `[requirements]` — Requirements clarity, acceptance criteria, task description
  - `[code]` — Code patterns, rules, architecture guidance

  Examples: `[system] e2e MCP tool reported test passed but it actually failed` or `[requirements] Feature requirements didn't specify mobile viewport testing`

⚠️ Your session terminates IMMEDIATELY after calling CompleteWork.

---

## Critical Rules

1. **Tests MUST pass** — Never approve failing tests
2. **No sleep statements** — This is non-negotiable
3. **Follow step pattern** — Every step needs action + verification
4. **One test per feature** — Avoid test proliferation
5. **Speed matters** — Reject slow, inefficient tests
