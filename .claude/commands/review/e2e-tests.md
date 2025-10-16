---
description: Review E2E test implementation for a Product Increment task
args:
  - name: title
    description: Task title to review (e.g., "End-to-end tests for team management")
    required: false
---

# Review E2E Tests Workflow

You are reviewing: **{{{title}}}**

## STEP 0: Read Task Assignment

**Read `current-task.json` in your workspace root** to get:
- `request_file_path`: Full path to engineer's request file
- `product_increment_path`: Path to Product Increment (if applicable)
- `title`: Task title

**Then read the request and response files** to understand what was implemented.

---

## STEP 1: Create Todo List

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
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing if approved"}
  ]
}
```

---

## Workflow Steps

**STEP 1**: Read all context

**STEP 2**: Run watch and e2e tools
- Use **watch MCP tool** to restart server and run migrations
- Use **e2e MCP tool** to run tests: `e2e(searchTerms=["feature-name"])`
- **ALL tests MUST pass to approve**
- If tests fail: REJECT immediately

**STEP 3**: Study E2E rules
- Read [End-to-End Tests](/.claude/rules/end-to-end-tests/e2e-tests.md)
- Ensure engineer followed all patterns

**STEP 4**: Review test file structure

**Critical Check 1 - Test Count:**
- Normally ONE new `@comprehensive` test per feature
- Existing `@smoke` tests should be updated, not duplicated
- For BIG features: Allow both new `@smoke` and new `@comprehensive`
- **REJECT if too many small test files created**

**STEP 5**: Review each test step

**Critical Check 2 - Step Naming Pattern:**
- **EVERY step MUST follow**: "Do something & verify result"
- ✅ Good: `"Submit login form & verify authentication"`
- ❌ Bad: `"Verify button is visible"` (no action)
- ❌ Bad: `"Test login"` (uses "test" prefix)
- **REJECT if steps don't follow pattern**

**Critical Check 3 - No Sleep Statements:**
- Search for: `waitForTimeout`, `sleep`, `delay`, `setTimeout`
- **REJECT immediately if found - no exceptions, no discussion**
- Playwright auto-waits - sleep is NEVER needed in any scenario
- Demand Playwright await assertions instead:
  - Use `toBeVisible()`, `toHaveURL()`, `toContainText()`, etc.
  - These built-in auto-wait mechanisms handle all timing scenarios

**STEP 6**: Review test efficiency

**Critical Check 4 - Leverage Existing Logic:**
- Verify tests use fixtures: `{ page }`, `{ ownerPage }`, `{ adminPage }`, `{ memberPage }`
- Verify tests use helpers: `expectToastMessage`, `expectValidationError`, etc.
- **REJECT if tests duplicate existing logic**

**Critical Check 5 - Speed Optimization:**
- Tests should test MANY things in FEW steps
- Avoid excessive navigation or setup
- Group related scenarios together
- **REJECT if tests are unnecessarily slow**

**STEP 7**: Decide - APPROVED or NOT APPROVED

If ANY critical check fails: **REJECT**

**STEP 8**: If APPROVED, run `/review/commit`

**STEP 9**: Signal completion

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

**Call MCP CompleteAndExitReview tool**:
- `agentType`: test-automation-reviewer
- `approved`: true or false
- `reviewSummary`: Objective description (e.g., "Add E2E tests for team management", "Fix sleep statements in user tests")
- `responseContent`: Your full review feedback

⚠️ Your session terminates IMMEDIATELY after calling CompleteAndExitReview

---

## CRITICAL RULES

1. **Tests MUST pass** - Never approve failing tests
2. **No sleep statements** - This is non-negotiable
3. **Follow step pattern** - Every step needs action + verification
4. **One test per feature** - Avoid test proliferation
5. **Speed matters** - Reject slow, inefficient tests
