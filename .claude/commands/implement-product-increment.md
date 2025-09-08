---
description: Implement tasks defined in a product increment with mandatory code review
argument-hint: Path to product increment .md file to implement (e.g., task-manager/2025-01-15-feature/1-backend.md)
---

# Implement Product Increment Workflow

Product increment file: $ARGUMENTS

## ⚠️ CRITICAL: Rules are MANDATORY - The #1 Success Factor

**THE MOST IMPORTANT THING**: Before implementing ANYTHING, you MUST thoroughly study ALL relevant rules. Rules are not guidelines or suggestions - they are ABSOLUTE REQUIREMENTS. Every single line of code must comply with the rules. The code reviewer will reject ANY deviation, no matter how minor. Success depends on following rules PRECISELY.

Read the product increment file provided in the arguments above. Your job is to implement the tasks defined in that specific product increment. If no product increment is provided in the arguments, inspect all product increments to find the first product increment that has not been fully completed.

The product increment contains tasks with several subtasks that must be implemented one by one. After a task and all its subtasks are implemented, you must build and test the code, request code review from the appropriate sub-agent, and only after approval change the task status from [Planned] to [Completed]. Each task should result in a separate commit.

## CRITICAL: Understanding vertical slices

**EACH TASK = ONE COMMIT = ONE VERTICAL SLICE**

A vertical slice means the task contains everything needed to deliver a working feature:
- Backend: Database migration + domain model + command/query + API endpoint + API tests
- Frontend: Component + API integration + state management + user interactions
- E2E: Complete test scenario with all assertions (separate product increment)

**The code must compile, run, and be independently testable after EACH task.**

## Workflow

Follow these steps which describe in detail how you must implement the tasks in the provided product increment.

## Before implementing the first task

### 1. Read the PRD to understand what we are building

- Read the entire PRD to understand the feature's purpose, scope, and business value
- Understand how this feature fits into the larger system
- Note any specific requirements or constraints mentioned

### 2. Read all product increment plans

- Read ALL product increment files to understand the complete implementation strategy
- Understand how the increments build upon each other
- Identify dependencies between increments
- Get a mental model of the entire feature before starting implementation

## For each task

### 1. Study the relevant rules FIRST - CRITICAL FOR SUCCESS

**THIS IS THE MOST IMPORTANT STEP - NEVER SKIP OR SHORTCUT**

Before doing ANYTHING else, thoroughly study ALL relevant rules for this task:

- **For backend tasks**: Review ALL files in [Backend Rules](/.claude/rules/backend/) 
  - Start with backend.md for general patterns
  - Then specific rules for your task type (commands.md, queries.md, api-endpoints.md, etc.)
- **For frontend tasks**: Review ALL files in [Frontend Rules](/.claude/rules/frontend/)
  - Start with frontend.md for core patterns
  - Then specific rules (react-aria-components.md, tanstack-query-api-integration.md, etc.)
- **For E2E test tasks**: Review [E2E Testing Rules](/.claude/rules/end-to-end-tests/)
- **For CLI tasks**: Review [Developer CLI Rules](/.claude/rules/developer-cli/)

**Why this is CRITICAL:**
- Rules encode years of best practices and hard-won knowledge
- Following rules prevents costly mistakes and rework
- Rules ensure consistency across the entire codebase
- Code reviewers will reject ANY deviation from rules
- Patterns in existing code may be outdated - rules are the source of truth

**Take time to internalize the rules. Success depends on following them precisely.**

### 2. Research codebase and validate approach

**Think ultrahard at this step.**

- Research the existing codebase to understand what needs to be made in this task
- Study similar features and their implementation patterns
- Validate that the suggested subtasks are still the correct way to implement
- **IMPORTANT**: If existing code conflicts with rules, follow the rules, not the code
- If the approach needs adjustment, update the task before starting implementation
- Identify exactly where new code should be placed

### 3. Implement subtasks one by one

For **each subtask**:

- **RE-READ the specific rule for THIS subtask (MANDATORY)**:
  - Even though you studied rules in step 1, RE-READ the specific rule for this exact subtask
  - For backend subtasks: Re-read the EXACT rule file (e.g., if creating a command, re-read commands.md)
  - For frontend subtasks: Re-read the EXACT rule file (e.g., if using React Aria, re-read react-aria-components.md)
  - For E2E test subtasks: Re-read [E2E Testing Rules](/.claude/rules/end-to-end-tests/e2e-tests.md)
  - For CLI subtasks: Re-read [Developer CLI Rules](/.claude/rules/developer-cli/)
  - **This is NOT optional - rules MUST be followed precisely**

- **Look at similar code (but rules take precedence)**:
  - Find other places in the code doing something similar
  - Use existing code as reference for patterns
  - **CRITICAL**: If code contradicts rules, ALWAYS follow rules, not the code
  - Existing code may be legacy or incorrect - rules are the authority

- **Implement the subtask following rules EXACTLY**:
  - Write detailed, clean, production-quality code
  - Follow rules PRECISELY - no shortcuts, no "close enough"
  - Every line must comply with the rules
  - Ensure the subtask is thoroughly completed before proceeding to the next

### 4. Validate implementation before code review

After completing **all subtasks** in a **backend** task:

- Write new tests to ensure new code coverage, including all edge cases
- Run `[CLI_ALIAS] build && [CLI_ALIAS] test` to verify the code compiles and all tests pass
- Fix any build or test failures before proceeding

After completing **all subtasks** in a **frontend** task:

- Run `[CLI_ALIAS] check --frontend` to verify TypeScript compilation and linting
- Fix any TypeScript or linting errors before proceeding

**Do not skip or shortcut these validation steps.**

### 5. Code review by sub-agent

**CRITICAL: This step is mandatory before marking any task as completed. NO EXCEPTIONS.**

After validating the implementation:

- For **backend tasks**: Launch the `backend-code-reviewer` agent
- For **frontend tasks**: Launch the `frontend-code-reviewer` agent
- For **e2e test tasks**: Launch the `e2e-test-reviewer` agent

**The reviewer will SPECIFICALLY check that ALL rules are followed perfectly.**

When launching the review agent, provide:
1. Link to the Product Increment file (e.g., `@.task-manager/yyyy-MM-dd-feature/1-product-increment.md`)
2. The task number just completed (e.g., "task 3")
3. Summary of changes made
4. If this is a follow-up review, link to previous review (e.g., `@.task-manager/yyyy-MM-dd-feature/1-product-increment/3-review.md`)

Example initial review:
```
"Review task 3 implementation from @.task-manager/2025-01-15-team-management/1-backend.md. 
Changes: Created Team aggregate with TeamId, added TeamRepository, configured EF mappings, 
created database migration, implemented CreateTeam command with validation, added POST endpoint, 
wrote comprehensive API tests."
```

Example follow-up review:
```
"Follow-up review for task 3 from @.task-manager/2025-01-15-team-management/1-backend.md.
Previous review: @.task-manager/2025-01-15-team-management/1-backend/3-review.md
Fixed: Corrected property ordering, removed defensive coding, fixed line wrapping, 
changed to primary constructor, used TimeProvider.System.GetUtcNow()"
```

The review agent will:
- Write findings to `.task-manager/yyyy-MM-dd-[prd-title]/[#-product-increment-title]/[task-#]-review.md`
- Document all issues found with line numbers and specific corrections needed
- Provide a summary with counts of critical, major, and minor issues

**Review standards - ZERO TOLERANCE:**
- ALL issues must be fixed, no matter how minor
- This includes spacing, line breaks, naming conventions, property ordering
- There is no such thing as "too critical" - perfection is the standard
- Reviews will continue indefinitely until ZERO issues remain

**If the review finds issues:**
1. Fix ALL issues identified by the review agent (including minor ones)
2. Re-validate the implementation (rebuild, retest, reformat)
3. Request a follow-up review, referencing the previous review file
4. The reviewer will see it's a follow-up and can check what was fixed
5. Repeat until the review agent approves with ZERO issues

**Only after review approval:**
- Proceed to final validation

### 6. Final validation and commit

**After review approval, before committing**:

For **backend** tasks:
- Run `[CLI_ALIAS] format --backend && [CLI_ALIAS] inspect --backend`
- Fix any formatting or inspection issues found
- If only trivial fixes were needed (formatting, simple inspection issues), proceed to commit
- If any non-trivial issues were fixed, re-run validation commands

For **frontend** tasks:
- Run `[CLI_ALIAS] format --frontend`
- Fix any formatting issues found
- Proceed to commit

For **E2E test** tasks:
- Run `[CLI_ALIAS] e2e` to verify tests pass
- If any issues are found and fixed (especially if using conditional logic or await statements), request another review
- Only proceed to commit after tests pass

**Commit the code**:
- Commit the implemented code according to [Commit Changes Workflow](/.windsurf/workflows/commit-changes.md)
- The commit message should describe the complete vertical slice implemented
- Each task should result in a separate commit

### 7. Reflect and adjust plans when needed

**After implementation, reflect on what was learned:**

- **Update the current task** in the product increment file to reflect any changes made while implementing
- **Update future tasks** if you discovered:
  - A future task was actually needed as part of this task (mark it as completed)
  - New tasks need to be added based on discoveries during implementation
  - Existing future tasks need to be modified or removed
  - Dependencies have changed

- **If significant changes were made**:
  - Update the product increment files to reflect the new reality
  - If scope changed significantly, update the PRD
  - Communicate changes to the user with clear explanations

This reflection ensures plans stay accurate and implementable.

### 8. Mark the current task as [Completed]

- Change the task header from [Planned] to [Completed] in the product increment file
- This indicates the task has been implemented, reviewed, and committed

### 9. Repeat process for next task

- Return to step 1 (Research codebase) for the next task
- Continue until all tasks in the product increment are completed
- Tasks must be completed sequentially, not in parallel

## Example implementation flow with follow-up review

Using the provided `1-backend-sample.md` example:

### **Task 1: Create Team aggregate, command, endpoint, migration, and tests [Planned]**

1. **Research codebase**: Study existing aggregates, commands, API patterns, validate approach
2. **Implement subtasks** (studying rules for each subtask):
   - 1.1 Create Team aggregate with TeamId, properties, Create method, ITenantScoped
   - 1.2 Create TeamRepository inheriting from ICrudRepository
   - 1.3 Configure strongly typed IDs via TeamConfiguration
   - 1.4 Create database migration with unique index
   - 1.5 Create CreateTeam command with validation, guards, event tracking
   - 1.6 Create POST /api/account-management/teams endpoint
   - 1.7 Create comprehensive API tests
3. **Validate**: Run `[CLI_ALIAS] build && [CLI_ALIAS] test`
4. **Initial Code Review**: 
   ```
   Launch backend-code-reviewer agent:
   "Review task 1 implementation from @.task-manager/2025-01-15-team-management/1-backend.md.
   Changes: Created Team aggregate with TeamId strongly typed ID, TeamRepository with 
   ICrudRepository, EF configuration, database migration with unique index, CreateTeam 
   command with Owner permission guard and uniqueness check, POST endpoint, and API tests."
   ```
5. **Review finds issues**: Review written to `1-review.md` with 15 issues found
6. **Fix all issues**: Address every single issue, including minor spacing problems
7. **Follow-up review**:
   ```
   Launch backend-code-reviewer agent:
   "Follow-up review for task 1 from @.task-manager/2025-01-15-team-management/1-backend.md.
   Previous review: @.task-manager/2025-01-15-team-management/1-backend/1-review.md
   Fixed: All 15 issues including property ordering, line spacing, primary constructors, 
   removed defensive coding, fixed DateTime usage, corrected naming conventions"
   ```
8. **Review finds 3 more issues**: Same file updated with new findings
9. **Fix remaining issues**: Address the 3 remaining issues
10. **Final follow-up review**: Request review again until ZERO issues
11. **Final validation**: Run `[CLI_ALIAS] format --backend && [CLI_ALIAS] inspect --backend`
12. **Commit**: Create commit with message like "Add team creation with aggregate, command, API endpoint and tests"
13. **Reflect**: Update task description if needed, adjust future tasks if discoveries were made
14. **Mark completed**: Change task status to [Completed]

### **Task 2: Create GetTeam query [Planned]**
- Repeat entire process for next task
- This is a smaller task since the foundation is already in place

## Review process details

The review agents will check for:
- Adherence to ALL rule files
- Consistent patterns with existing code
- Proper use of language features (primary constructors, sealed classes, etc.)
- Correct ordering of properties and parameters
- Appropriate error handling patterns
- No defensive coding
- Minimal and meaningful comments
- Proper line wrapping and formatting
- Correct spacing and line breaks
- Naming conventions (no acronyms, clear names)
- Security and performance considerations
- Use of TimeProvider.System.GetUtcNow() instead of DateTime.UtcNow()
- Proper async/await usage
- No XML comments
- Logging without periods, exceptions with periods

Review findings are documented with:
- Exact line numbers
- Specific issues
- Suggested corrections
- Severity classification (critical/major/minor)
- ALL issues must be fixed regardless of severity

## Important notes

### Sequential execution
Tasks must be completed one at a time, sequentially. Parallel implementation is not allowed as it leads to conflicts and compilation errors.

### E2E tests as separate product increment
E2E tests should always be a separate product increment, created after the feature is complete and stable. E2E tests require special attention to ensure they are deterministic, use proper Playwright assertions, and never use hardcoded waits or conditional logic.

### Scope limitations
This workflow is for implementing new features through product increments. It is not for:
- Bug fixes
- Hotfixes
- Small changes
- Emergency repairs

### Documentation
README updates should be considered when creating product increment plans. Documentation updates are part of the pull request process.

## ✅ DO:
- Read the PRD and ALL product increments before starting implementation
- **ALWAYS study ALL relevant rules FIRST before starting any task**
- Think ultrahard when researching codebase for each task
- **Re-read specific rule files for EACH subtask (not just once per task)**
- **Follow rules EXACTLY - they override any conflicting code patterns**
- Look at similar code for reference but rules take precedence
- Implement subtasks sequentially for each vertical slice
- Always validate code (build, test) before requesting code review
- ALWAYS request code review from sub-agents before marking tasks complete
- Fix ALL issues from code reviews, no matter how minor
- Continue review cycles until ZERO issues remain
- Always run formatting and inspection after review approval
- Commit code immediately after final validation passes
- Reflect and update plans AFTER implementation to capture learnings
- Mark tasks as [Completed] only after commit
- Ensure each commit is a deployable and tested unit of work
- Update PRD and product increments when plans need adjustment

## ❌ DON'T:
- Skip reading the PRD and all product increments before starting
- **EVER skip studying rules first - this is the #1 cause of failure**
- Skip researching the codebase and validating approach for each task
- **Review rules only once for the entire task (must re-read per subtask)**
- **Follow code patterns that contradict rules - rules are absolute**
- Implement multiple tasks or subtasks simultaneously
- Proceed to the next task before getting code review approval with ZERO issues
- Mark a task as [Completed] before committing
- Ignore minor issues like spacing or naming conventions
- Skip writing and running comprehensive tests, including edge cases
- Skip the pre-review validation commands
- Proceed without running formatting and inspection after review
- Forget to reflect and update plans after implementation
- Continue without following explicit rules defined in [Rules](/.windsurf/rules)
- Split a vertical slice into multiple commits (database, backend, API, tests separately)
- Work on tasks in parallel
- Use this workflow for bug fixes or small changes

**This process ensures a rigorous, thoroughly reviewed, and user-confirmed workflow for high-quality and reliable implementation with ZERO tolerance for imperfection.**