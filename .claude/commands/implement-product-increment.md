---
description: Implement tasks defined in a product increment with mandatory code review
argument-hint: Path to PRD (feature/prd.md) and/or paths to a product increment file to implement (e.g., task-manager/2025-01-15-feature/1-backend.md)
---

# Implement Product Increment Workflow

PRD and/or Product Increment file(s): $ARGUMENTS

If you only get the PRD search for *.md files in the same directory as the PRD and implement them.

## Critical: This workflow is MANDATORY - Follow every step exactly

**EACH TASK = ONE COMMIT = ONE VERTICAL SLICE**

You MUST complete tasks sequentially. Each task must be implemented, reviewed to ZERO issues, validated, and committed before moving to the next task. You cannot move to the next Product Increment until ALL tasks in the current one are [Completed].

**The code must compile, run, and be independently testable after EACH task.**

## Critical: Always use [CLI_ALIAS] - NO EXCEPTIONS

**MANDATORY**: Use `[CLI_ALIAS] build`, `[CLI_ALIAS] test`, `[CLI_ALIAS] format`, `[CLI_ALIAS] inspect`, `[CLI_ALIAS] e2e`
**FORBIDDEN**: `dotnet`, `npm`, `npx` commands - NEVER fall back to these even if [CLI_ALIAS] appears to fail
**CLI TOOL WORKS**: It's globally available and has system knowledge - trust it completely

## Mandatory workflow - Use todo list to force compliance

**FIRST ACTION: Create high-level todo list with ALL Product Increments you need to implement**

Example for teams feature with 5 Product Increments:
```
### Product Increment 1: Backend team management
### Product Increment 2: Frontend team management  
### Product Increment 3: Backend team membership
### Product Increment 4: Frontend team membership
### Product Increment 5: End-to-end testing
```

**WHEN STARTING A PRODUCT INCREMENT: Add all tasks as subtasks**

Example when starting Product Increment 1:
```
### Product Increment 1: Backend team management [in_progress]
## 1. Create team aggregate with database migration and CreateTeam command
## 2. Create GetTeam query for retrieving single team
## 3. Create GetTeams query for listing all teams  
## 4. Create UpdateTeam command for modifying team details
## 5. Create DeleteTeam command for removing teams
### Product Increment 2: Frontend team management
### Product Increment 3: Backend team membership
### Product Increment 4: Frontend team membership
### Product Increment 5: End-to-end testing
```

**WHEN STARTING A TASK: Add all workflow steps as sub-sub items**

Example when starting Backend Task 2 (assume Task 1 is completed):
```
### Product Increment 1: Backend team management [in_progress]
## 1. Create team aggregate with database migration and CreateTeam command [completed]
## 2. Create GetTeam query for retrieving single team [in_progress]
- Study ALL rules for this task type                                                      (STEP 2)
- Research existing patterns for this task type                                           (STEP 3)
- 2.1 Create TeamDetails response model                                                   (STEP 4)
- 2.2 Create GetTeam query                                                                (STEP 4)
- 2.3 Create GET endpoint /api/teams/{id}                                                 (STEP 4)
- 2.4 Create integration tests                                                            (STEP 4)
- Validate implementation builds: Run [CLI_ALIAS] build && [CLI_ALIAS] test               (STEP 5)
- MANDATORY CODE REVIEW: Call backend-code-reviewer agent                                 (STEP 6)
- MANDATORY QUALITY GATES: Call quality-gate-committer agent with task description and review file (STEP 7)
- Update Product Increment status                                                         (STEP 8)
## 3. Create GetTeams query for listing all teams
## 4. Create UpdateTeam command for modifying team details  
## 5. Create DeleteTeam command for removing teams
### Product Increment 2: Frontend team management
### Product Increment 3: Backend team membership
### Product Increment 4: Frontend team membership
### Product Increment 5: End-to-end testing
```

**UPDATE TODO LIST CONTINUOUSLY**: Mark items [completed], [in_progress], [pending] as you work through them.

## Mandatory workflow - Every step must be in your todo list

**YOU MUST USE TodoWrite tool at each major step to track progress and prevent skipping steps**

### For each task, your todo list MUST include all these items (match step numbers):

**Backend Task Example:**
```
## [Task Number]. [Task Title] [in_progress]
- Update status in Product Increment markdown file to [In Progress]                       (STEP 1)
- Study ALL rules for this task type                                                      (STEP 2)
- Research existing patterns for this task type                                           (STEP 3)
- [List all subtasks from Product Increment plan]                                         (STEP 4)
- Validate implementation builds: Run [CLI_ALIAS] build && [CLI_ALIAS] test               (STEP 5)
- MANDATORY CODE REVIEW: Call backend-code-reviewer agent                                 (STEP 6)
- MANDATORY QUALITY GATES: Call quality-gate-committer agent with task description and review file (STEP 7)
- Update Product Increment status                                                         (STEP 8)
```

**Frontend Task Example:**
```
## [Task Number]. [Task Title] [in_progress]
- Update status in Product Increment markdown file to [In Progress]                       (STEP 1)
- Study ALL rules for this task type                                                      (STEP 2)
- Research existing patterns for this task type                                           (STEP 3)
- [List all subtasks from Product Increment plan]                                         (STEP 4)
- Validate implementation builds: Run [CLI_ALIAS] check --frontend                        (STEP 5)
- MANDATORY CODE REVIEW: Call frontend-code-reviewer agent                                (STEP 6)
- MANDATORY QUALITY GATES: Call quality-gate-committer agent with task description and review file (STEP 7)
- Update Product Increment status                                                         (STEP 8)
```

**E2E Task Example:**
```
## [Task Number]. [Task Title] [in_progress]
- Update status in Product Increment markdown file to [In Progress]                       (STEP 1)
- Study ALL rules for this task type                                                      (STEP 2)
- Research existing patterns for this task type                                           (STEP 3)
- [List all subtasks from Product Increment plan]                                         (STEP 4)
- Validate implementation builds: Run [CLI_ALIAS] e2e                                     (STEP 5)
- MANDATORY CODE REVIEW: Call e2e-test-reviewer agent                                     (STEP 6)
- MANDATORY QUALITY GATES: Call quality-gate-committer agent with task description and review file (STEP 7)
- Update Product Increment status                                                         (STEP 8)
```

### Mandatory steps for every task:

**Step 1. Update todo list and mark task in progress**: 
   - Add all workflow steps for current task (use template above)
   - IMPORTANT: Mark task as [In progress] in the Product Increment .md file

**Step 2. Study all rules for this task type**: 
   - **Backend**: Read ALL files in /.claude/rules/backend/
   - **Frontend**: Read ALL files in /.claude/rules/frontend/  
   - **E2E**: Read ALL files in /.claude/rules/end-to-end-tests/
   - Mark "Study ALL rules for this task type" [completed] in todo

**Step 3. Research existing patterns for this task type**: 
   - Study similar implementations in codebase
   - Validate approach matches established patterns
   - Mark "Research existing patterns for this task type" [completed] in todo  

**Step 4. Implement all subtasks**: 
   - Complete each subtask listed in Product Increment plan
   - Mark each individual subtask [completed] in todo as you finish it

**Step 5. Validate implementation builds - ZERO TOLERANCE gate**: 
   - **Mandatory gate**: Commands MUST achieve 100% success with zero errors/warnings
   - **Backend tasks**: Run `[CLI_ALIAS] build && [CLI_ALIAS] test` - all MUST pass
   - **Frontend tasks**: Run `[CLI_ALIAS] check --frontend` - all MUST pass  
   - **E2E tasks**: Run `[CLI_ALIAS] e2e` - all MUST pass
   - **Gate rule**: You CANNOT proceed to Step 6 until every single build/test passes
   - **No exceptions**: If failures occur, troubleshoot and fix until 100% success
   - **Completion criteria**: Mark completed ONLY when commands show zero failures

**Step 6. Mandatory code review - iterative until ZERO findings**:
   - **Backend tasks**: Call `backend-code-reviewer` agent
   - **Frontend tasks**: Call `frontend-code-reviewer` agent
   - **E2E tasks**: Call `e2e-test-reviewer` agent
   - Agent creates: task-manager/feature/#-product-increment/#-review.md with findings
   - **ZERO TOLERANCE rule**: You MUST fix ALL findings - no exceptions, no "minor" issues
   - **Before follow-up reviews**: Re-run ALL Step 5 validation builds to ensure fixes didn't break anything
   - **Iterative process**: Call reviewer agent repeatedly until zero findings remain
   - **Validation requirement**: Mark fixed items with [x] in review.md
   - **Disagreement protocol**: If you disagree with a finding, document reasoning in review.md
   - **Gate rule**: You CANNOT proceed to Step 7 until review.md shows [x] on ALL findings and agent agrees

**Step 7. Final quality gates and commit via quality-gate-committer agent - MANDATORY**:
   - **PREREQUISITES CHECK**: Before calling the agent, you MUST verify:
     - ALL review findings in task-manager/feature/#-product-increment/#-review.md are marked [x]
     - ALL build/test commands from Step 5 pass with zero failures
     - If either prerequisite fails, DO NOT call the agent - fix issues first
   - **Call quality-gate-committer agent**: 
     - Provide task description for commit message
     - Provide path to review file: task-manager/feature/#-product-increment/#-review.md
     - Agent will validate review completion, run quality gates, and commit if all pass
   - **HARD GATE**: The agent will REJECT commits if:
     - Any review finding is not marked [x]
     - Any quality gate fails (build, test, lint, format)
     - No exceptions - fix all issues before retrying
   - **Success criteria**: Agent commits code and returns commit message
   - **Failure handling**: If agent fails, return to Step 5 to fix issues, then Step 6 for re-review

**Step 8. Update product increment status**: 
   - Change task from [Planned] to [Completed] in product increment markdown file
   - Mark all workflow steps [completed] in todo
   - If last task in Product Increment mark "Update Product Increment status" [completed] in todo

**CRITICAL**: You cannot start the next task until current task todo items are ALL [completed]

**IMPORTANT**: The new Step 7 (quality-gate-committer agent) will NEVER commit code that has:
- Failing tests or build errors
- Unaddressed review findings (not marked [x])
- Quality gate failures
This prevents the AI from ignoring test failures or review comments - a HARD ENFORCEMENT mechanism.

## Critical failure prevention

**Most common agent failures and how to prevent them:**

- **Skipping code review**: Agents implement code but forget to call review agents. This is FORBIDDEN.
- **Moving between increments**: Agents jump to next Product Increment before finishing current one. This is FORBIDDEN.
- **Ignoring build failures**: Agents continue despite compilation errors. This is FORBIDDEN.
- **Not updating status**: Agents forget to mark tasks [Completed] in .md files. This is FORBIDDEN.
- **Using wrong tools**: Agents fall back to `dotnet`, `npm`, `npx` instead of `[CLI_ALIAS]`. This is FORBIDDEN.

**Solution**: Follow the 9-step workflow exactly. No exceptions. No shortcuts. Always use [CLI_ALIAS].