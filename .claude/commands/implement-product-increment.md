---
description: Coordinates full implementation of a product increment via delegated subagents
---

## Inputs

- `$ARGUMENTS[0]`: Path to the Product Requirements Document (PRD) markdown file

## Context
- **PRD**: Product Requirements Document containing multiple product increments to implement
- **Product Increment**: A discrete, deliverable piece of functionality (e.g., "backend for user management")
- **Product Increment Plan**: Detailed implementation plan file for each increment (located in same directory as PRD)
- **CLI Tool**: The project uses `pp` (PlatformPlatform CLI) for all build/test/format operations

## Workflow
Your role: Architect and tech lead coordinating a senior development team.  
**Important**: You must **not** make any code changes yourself. Only coordinate and review.

### Phase 1: Discovery
1. **Find all product increments in the PRD** by reading `$ARGUMENTS[0]`.
2. **For each product increment found**, locate the corresponding product increment plan file in the same directory as `$ARGUMENTS[0]`.

### Phase 2: Implementation
For each product increment, execute these steps:

1. **Create a branch**:
   * Name format: `<increment-name>` (e.g., `backend-for-user-management`)
   * First increment: branch from `main`
   * Subsequent increments: branch from the previous increment's last commit (sequential stacking)

2. **Delegate development to a subagent**:
   * Provide the subagent with:
     - PRD path (`$ARGUMENTS[0]`)
     - Path to current product increment plan file
     - Clear scope: implement only the current task, not the entire increment
   * Instruct them to:
     - Read `.windsurf/rules/main.md` (mandatory) and relevant rule files
     - Research existing codebase to understand patterns and conventions
     - Implement the task
     - Use `pp` commands during development:
       - `pp build --backend` or `pp build --frontend` (depending on what changed) or `pp build` (both)
       - `pp test` (backend) or `pp e2e --quiet` (frontend)
     - Write new tests for backend features ensuring edge case coverage
   * After feature is complete run these slow commands:
     - `pp format --backend` or `pp format --frontend` or `pp format` (both)
     - `pp check --backend` or `pp check --frontend` or `pp check` (both)
   * If changes to plans are needed, update files with prefixes:
     - `UPDATED:` - Modified requirement
     - `DELETED:` - Removed requirement
     - `ADDED:` - New requirement discovered
     - `MOVED TO TASK #:` - Requirement belongs elsewhere

3. **Stage all changes** in Git before review.

4. **Delegate code review to another subagent**:
   * Provide reviewer with:
     - PRD path (`$ARGUMENTS[0]`)
     - Path to current product increment plan file
   * Reviewer must:
     - Review uncommitted changes file by file
     - Verify alignment with `.windsurf/rules` 
     - Check consistency with existing codebase patterns
     - Return detailed feedback or approval
     - **NOT make any code changes**
   * If approved: Ask reviewer to commit using `.windsurf/workflows/commit-changes.md`

5. **Handle review outcome**:
   * **If rejected**: Return to step 2 with review feedback
   * **If approved**: Verify all checks pass:
     - Run `pp check` and appropriate tests
     - Confirm all code changes are committed
     - If checks fail: Return to step 2

6. **Update tracking documents**:
   * Mark task as complete `[x]` in product increment plan
   * Note: PRD and increment plans are in `/task-manager` directory (separate git repository)
   * Commit updates with single-line imperative message

7. **Repeat for all tasks** in current product increment.

### Phase 3: Product Increment Completion
After completing all tasks in an increment:
1. **Evaluate plan adherence**:
   * If implementation closely followed the plan → Continue to next increment
   * If significant deviations occurred → **STOP and consult user**
2. **Proceed to next increment** (repeat Phase 2)

### Phase 4: Final Summary
When all increments are complete:
1. **List all completed increments** and their branches
2. **Confirm PRD requirements are fully implemented**
3. **Report completion status to user**
