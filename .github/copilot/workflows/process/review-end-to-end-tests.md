# Review E2E Tests Workflow

You are reviewing: **{{{title}}}**

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

- **Agentic mode**: The review request comes from `current-task.json`. The CLI passes only the task title as the slash command argument. You run autonomously without human supervision - work with your team to find solutions.
- **Standalone mode**: Test files are passed as command arguments `{{{title}}}`. Read test files from user-provided paths or from `git status`.

## Review Principles

**Zero Tolerance for Test Quality**: E2E tests must be perfect. ALL tests must pass, ZERO console errors, ZERO network errors, NO sleep statements. There are no exceptions.

**Evidence-Based Reviews**: Every finding must be backed by rules in `/.github/copilot/rules/end-to-end-tests/end-to-end-tests.md` or established patterns in the codebase.

**Speed is Critical**: Tests must run fast. Reject tests that are unnecessarily slow or create too many small test files.

---

## STEP 0: Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.github/copilot/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
   - `requestFilePath`: Request file path (contains engineer's request message)
   - `responseFilePath`: Response file path (where you'll write your review outcome)
   - `featureId`: [FeatureId] (the feature this task belongs to, or "ad-hoc" for ad-hoc work)
   - `taskId`: [TaskId] (the task being reviewed, or "ad-hoc-yyyyMMdd-HHmm" for ad-hoc work)
   - `taskTitle`: Task title

3. **Read the request file** from the path in `requestFilePath`.

4. **Read all files referenced in the engineer's request** (test files, implementation details, etc.).

5. **Create Todo List**

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Read [feature] and [task] to understand requirements", "status": "pending", "activeForm": "Reading feature and task"},
    {"content": "Run e2e tests and verify ALL pass with zero tolerance", "status": "pending", "activeForm": "Running E2E tests"},
    {"content": "Review test file structure and organization", "status": "pending", "activeForm": "Reviewing test structure"},
    {"content": "Review each test step for correct patterns", "status": "pending", "activeForm": "Reviewing test steps"},
    {"content": "Review test efficiency and speed", "status": "pending", "activeForm": "Reviewing test efficiency"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing if approved"},
    {"content": "Update [task] status to [Completed] or [Active]", "status": "pending", "activeForm": "Updating task status"},
    {"content": "MANDATORY: Call CompleteWork", "status": "pending", "activeForm": "Calling CompleteWork"}
  ]
}
```

---

## Workflow Steps

**STEP 1**: Read [feature] and [task] to understand requirements

1. **Read the [feature]** from `featureId` in [PRODUCT_MANAGEMENT_TOOL] (if not ad-hoc):
   - Understand the overall problem and solution approach.

2. **Read the [task]** from `taskId` in [PRODUCT_MANAGEMENT_TOOL]:
   - Read the task description carefully.
   - Understand what tests should cover.

3. **Read engineer's request** to understand what tests were created.

**If [task] lookup fails** (not found, already completed, or error): This is a coordination error. Report a problem and reject the review explaining the task could not be found.

4. **Study E2E rules**:
   - Read [End-to-End Tests](/.github/copilot/rules/end-to-end-tests/end-to-end-tests.md)
   - Ensure engineer followed all patterns

**STEP 2**: Run e2e tests and verify ALL pass with zero tolerance

**If tests require backend changes, run the run tool first**:
- Use **run MCP tool** to restart server and run migrations
- The tool starts .NET Aspire at https://localhost:9000

**Run E2E tests**:
- Use **e2e MCP tool** to run tests: `e2e(searchTerms=["feature-name"])`
- **ALL tests MUST pass with ZERO failures to approve**
- **Verify ZERO console errors** during test execution
- **Verify ZERO network errors** (no unexpected 4xx/5xx responses)
- If ANY test fails: REJECT
- If ANY console errors: REJECT
- If ANY network errors: REJECT

**STEP 3**: Review test file structure and organization

**Critical Check 1 - Test Count:**
- Normally ONE new `@comprehensive` test per feature
- Existing `@smoke` tests should be updated, not duplicated
- For BIG features: Allow both new `@smoke` and new `@comprehensive`
- **Reject if too many small test files created**

**STEP 4**: Review each test step for correct patterns

**Critical Check 1 - Step Naming Pattern:**
- **EVERY step MUST follow**: "Do something & verify result"
- ✅ Good: `"Submit login form & verify authentication"`
- ❌ Bad: `"Verify button is visible"` (no action)
- ❌ Bad: `"Test login"` (uses "test" prefix)
- **Reject if steps don't follow pattern**

**Critical Check 2 - No Sleep Statements:**
- Search for: `waitForTimeout`, `sleep`, `delay`, `setTimeout`
- **Reject if found—no exceptions**
- Playwright auto-waits—sleep is NEVER needed in any scenario
- Demand Playwright await assertions instead:
  - Use `toBeVisible()`, `toHaveURL()`, `toContainText()`, etc.
  - These built-in auto-wait mechanisms handle all timing scenarios

**STEP 5**: Review test efficiency and speed

**Critical Check 1 - Leverage Existing Logic:**
- Verify tests use fixtures: `{ page }`, `{ ownerPage }`, `{ adminPage }`, `{ memberPage }`
- Verify tests use helpers: `expectToastMessage`, `expectValidationError`, etc.
- **Reject if tests duplicate existing logic**

**Critical Check 2 - Speed Optimization:**
- Tests should test MANY things in FEW steps
- Avoid excessive navigation or setup
- Group related scenarios together
- **Reject if tests are unnecessarily slow**

**STEP 6**: Make binary decision (approve or reject)

**Aim for perfection, not "good enough".**

**APPROVED only if ALL criteria met:**
- ✓ All E2E tests passed with zero failures
- ✓ Zero console errors during test execution
- ✓ Zero network errors during test execution
- ✓ No sleep statements found
- ✓ All steps follow "Do something & verify result" pattern
- ✓ Tests use existing fixtures and helpers
- ✓ Tests are efficient and fast

**Reject if any issue exists—no exceptions. Common rationalizations to avoid:**
- ✗ "Test failed but feature works manually" → Reject, fix test
- ✗ "Console error unrelated to E2E code" → Reject anyway
- ✗ "It's just a warning" → Reject, zero means zero
- ✗ "Previous test run passed" → Reject anyway if current run has issues

**When rejecting:** Do full review first, then reject with ALL issues listed (avoid multiple rounds).

**STEP 7**: If approved, commit changes

1. Stage test files: `git add <file>` for each test file
2. Commit: One line, imperative form, no description, no co-author
3. Get hash: `git rev-parse HEAD`

Don't use `git add -A` or `git add .`

**STEP 8**: Update [task] status to [Completed] or [Active]

**If `featureId` is NOT "ad-hoc" (regular task from a feature):**
- If APPROVED: Update [task] status to [Completed].
- If REJECTED: Update [task] status back to [Active].

**If `featureId` is "ad-hoc" (ad-hoc work):**
- Skip [PRODUCT_MANAGEMENT_TOOL] status updates.

**STEP 9**: Call CompleteWork

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

## Rules

1. **Tests must pass** — Don't approve failing tests
2. **No sleep statements** — Non-negotiable
3. **Follow step pattern** — Every step needs action + verification
4. **One test per feature** — Avoid test proliferation
5. **Speed matters** — Reject slow, inefficient tests
