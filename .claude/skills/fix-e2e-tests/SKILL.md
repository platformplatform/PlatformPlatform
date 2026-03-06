---
name: fix-e2e-tests
description: Systematically fix all failing E2E tests using a phased diagnostic approach. Classifies tests as passing, flaky, or permanently failing, then fixes them one by one with progressive scope expansion.
allowed-tools: Read, Write, Edit, Bash, Glob, Grep, mcp__developer-cli__end_to_end, mcp__developer-cli__build, mcp__developer-cli__run
---

# Fix E2E Tests

Systematically fix all failing end-to-end tests by diagnosing first, then fixing one by one, expanding scope progressively. Optimizes for speed of fixing over exhaustive analysis.

## Core Principles

1. **Test bug or app bug?** For every failure, critically evaluate: is the test wrong, or is the application wrong? A failing test might be correctly catching an application bug. If the application is broken, fix the application -- do not make the test pass by weakening it
2. **Focus on the first failing step only.** Do not attempt to fix later steps in a test until the first step passes. You do not know if later steps would actually fail
3. **Never predict failures.** Do not speculatively fix things that look like they might fail. Only fix what is actually failing right now
4. **Apply global fixes.** When a fix applies across multiple tests (e.g., a renamed button), apply it everywhere before re-running. Do not fix one test at a time when the same change applies to many
5. **One test at a time.** After global fixes, run each failing test individually. Fix the first step. Re-run. Iterate

## STEP 1: Diagnostic Run -- Smoke Tests in Chromium

Run all smoke tests in Chromium with retries=1 to classify every test:
- **Passing**: Passes on first attempt
- **Flaky**: Fails first attempt, passes on retry
- **Permanently failing**: Fails both attempts

Save a diagnostic report to `.workspace/{branch-name}/e2e-diagnostic.md` with test counts and, for each failure, the test file, test name, first failing step, and error message.

If all tests pass, skip to STEP 4.

## STEP 2: Fix Permanently Failing Tests

Work through permanently failing tests one at a time:

1. **Run the failing test in isolation** to confirm it is truly permanently failing and not flaky. A test that failed in the full suite might pass when run alone (resource contention, test ordering). If it passes in isolation, reclassify it as flaky and handle in STEP 3
2. **Read the first failing step** and its error message
2. **Evaluate: test bug or app bug?** Did we deliberately change something the test is catching? Would a real user see this as broken? If it is an app bug, report it and move on -- do not fix the test
3. **Check for global applicability.** Does the same fix apply to other tests? Apply globally first, then re-run diagnostics
4. **Fix the first step.** Make the minimal change. Do not touch later steps
5. **Re-run the individual test.** If it passes, move to the next failing test. If a new step fails, repeat from step 1

Update the diagnostic report after each fix.

## STEP 3: Address Flaky Tests

After permanently failing tests are fixed:

1. Run each flaky test multiple times individually to confirm flakiness
2. Identify the root cause -- do not add arbitrary waits or timeouts
3. Review the E2E test rules in `.claude/rules/end-to-end-tests/` for known patterns
4. Fix the root cause. If the flakiness is caused by an application bug, report it to the user -- the application must be fixed
5. Re-run the test multiple times to confirm it is now stable. Do not move on until it passes consistently

## STEP 4: Expand to All Browsers -- Smoke Tests

Run smoke tests across all browsers. Fix any browser-specific failures using the same one-at-a-time process from STEP 2.

## STEP 5: Expand to All Tests in Chromium

Run all tests (smoke + comprehensive, excluding slow) in Chromium. Fix any failures using the same process.

## STEP 6: Expand to All Tests in All Browsers

Run the full test suite across all browsers. Fix any remaining failures.

## STEP 7: Final Validation

Run the full test suite across all browsers one final time. Every test must pass. Zero failures, zero flaky tests. This skill is not complete until all tests pass consistently.

Update `.workspace/{branch-name}/e2e-diagnostic.md` with:
- Final test counts: all must be passing
- Summary of changes made (test fixes and application fixes)
- Application bugs that were found and fixed

## Key Rules

- Run tests for the specific failing file, not the whole suite, when fixing individual tests
- Apply global fixes before re-running diagnostics
- Do not run all browsers until Chromium passes
- Only fix what is actually failing -- do not refactor passing tests
- Read `.claude/rules/end-to-end-tests/` before making changes
- If stuck after 3 fix attempts on the same test, escalate to the user
- Zero tolerance: this skill is not complete until every test passes in every browser
