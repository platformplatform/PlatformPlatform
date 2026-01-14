---
name: update-flaky-tests
description: Update the flaky test tracker. Use when you encounter test failures unrelated to your current work, after committing a fix for a known flaky test, or to check flaky test status.
allowed-tools: Read, Write, Bash, Glob
---

# Update Flaky Tests

Track and manage flaky E2E test observations over time. This skill helps systematically log test failures that are unrelated to the current work, preserving error artifacts for later analysis.

## STEP 1: Load Database

Read the flaky tests database from `.workspace/flaky-tests/flaky-tests.json`.

If the file or folder doesn't exist:
1. Create the folder structure: `.workspace/flaky-tests/` and `.workspace/flaky-tests/artifacts/`
2. Initialize the database using the schema at `/.claude/skills/update-flaky-tests/flaky-tests-schema.json`
3. Create the main database file (`.workspace/flaky-tests/flaky-tests.json`):
```json
{
  "lastUpdated": "<current UTC timestamp>",
  "active": []
}
```
4. Create an empty archive file (`.workspace/flaky-tests/flaky-tests-archived.json`):
```json
{
  "lastUpdated": "<current UTC timestamp>",
  "active": []
}
```

## STEP 2: Auto-Maintenance

Perform automatic maintenance on every run:

1. Find tests in `active` array with status `fix-applied` where `lastSeen` is more than 7 days ago
2. Move these tests to the archive file at `.workspace/flaky-tests/flaky-tests-archived.json`
   - If archive file doesn't exist, create it with same structure: `{ "lastUpdated": "...", "active": [] }`
   - Append tests to the archive's `active` array
   - Remove tests from the main database's `active` array
3. Report any auto-archived tests: "Auto-archived X tests that have been stable for 7+ days: [test names]"

## STEP 3: Determine Context

Assess what action is needed based on your current context:

| Context | Mode |
|---------|------|
| Just ran E2E tests with failures | **Log mode** |
| Just committed a fix for a known flaky test | **Fix mode** |
| Neither / standalone check | **Status mode** |

## STEP 4: Execute Based on Mode

### Log Mode (after test failures)

For each test failure you observed:

1. **Classify the failure**:
   - Is it related to your current work? Skip it (fix it as part of your task)
   - Is it unrelated (flaky)? Log it

2. **For unrelated failures, check if already tracked**:
   - Search `active` array for matching `testFile` + `testName` + `stepName` + `browser`
   - If found: increment `observationCount`, update `lastSeen`, add new observation
   - If not found: create new entry with status `observed`

3. **Preserve error artifacts**:
   - Find the error-context.md in `application/*/WebApp/tests/test-results/test-artifacts/`
   - Create timestamped folder: `.workspace/flaky-tests/artifacts/{timestamp}-{testFile}-{browser}-{stepName}/`
   - Copy error-context.md (and screenshots if present) to this folder
   - Store relative path in observation's `artifactPath` field

4. **Auto-promote status**:
   - If `observationCount` >= 2, change status from `observed` to `confirmed`

**Observation fields to populate**:
- `timestamp`: Current UTC timestamp (ISO 8601)
- `branch`: Current git branch
- `errorMessage`: The error message from the failure
- `artifactPath`: Relative path to preserved artifacts
- `observedBy`: Your agent type (qa-engineer, qa-reviewer, other)

### Fix Mode (after committing a flaky test fix)

1. Identify which flaky test was fixed (ask if unclear)
2. Find the test in the `active` array
3. Update the entry:
   - Set `status` to `fix-applied`
   - Populate the `fix` object:
     - `appliedAt`: Current UTC timestamp
     - `commitHash`: The commit hash of the fix
     - `description`: Brief description of what was fixed
     - `appliedBy`: Your agent type

### Status Mode (standalone check)

Read `/.claude/skills/update-flaky-tests/status-output-sample.md` first. Output status as a markdown table matching that format. Sort by Count descending. Omit Archived section if empty. End with legend line, nothing after.

## STEP 5: Save Database

1. Update `lastUpdated` to current UTC timestamp
2. Write the updated database to `.workspace/flaky-tests/flaky-tests.json`
3. Report changes made:
   - "Added X new flaky test observations"
   - "Updated X existing entries"
   - "Marked X tests as fix-applied"
   - "Auto-archived X resolved tests"

## Key Rules

**Only log tests you're confident are unrelated to your current work:**
- If the test fails in code you're changing, fix it - don't log it as flaky
- If you're unsure, err on the side of NOT logging

**Preserve artifacts for comparison:**
- What looks like the same flaky test might have subtle differences
- Always copy the error-context.md when logging

**Use local timestamps everywhere:**
- All `timestamp`, `lastSeen`, `appliedAt`, `lastUpdated` fields use local time
- Format: ISO 8601 without timezone suffix (e.g., `2026-01-14T14:30:00`)
- **Get current local time**: Run `date +"%Y-%m-%dT%H:%M:%S"` - never guess the time

## Reference Files

- **Schema**: `/.claude/skills/update-flaky-tests/flaky-tests-schema.json`
- **Sample database**: `/.claude/skills/update-flaky-tests/flaky-tests-sample.json`
- **Sample archive**: `/.claude/skills/update-flaky-tests/flaky-tests-archived-sample.json`
- **Sample status output**: `/.claude/skills/update-flaky-tests/status-output-sample.md`

**Test entry structure** (unique key = testFile + testName + stepName + browser):
```json
{
  "testFile": "account-management/WebApp/tests/e2e/user-management-flows.spec.ts",
  "testName": "should handle user invitation and deletion workflow",
  "stepName": "Delete user & verify confirmation dialog closes",
  "browser": "Firefox",
  "errorPattern": "confirmation dialog still visible after close",
  "status": "confirmed",
  "observations": [...],
  "lastSeen": "2026-01-14T10:30:00",
  "observationCount": 3,
  "fix": null,
  "notes": "Timing issue with dialog close animation"
}
```

**Status lifecycle**:
```
observed (1 observation) -> confirmed (2+ observations) -> fix-applied -> archived (7+ days stable)
```
