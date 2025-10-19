---
description: Orchestrate implementation of all Product Increments in a PRD through delegation to Product Increment coordinator subagents
argument-hint: Path to PRD (yyyy-MM-dd-feature/prd.md) and/or paths to Product Increment files (e.g., .workspace/task-manager/2025-01-15-feature/1-backend.md)
---

# Orchestrate PRD Implementation

PRD and/or Product Increment file(s): $ARGUMENTS

If you only get the PRD:
1. Read the PRD file using: `Read(file_path: "/path/to/prd.md")`
2. Find all Product Increment files using: `Glob(pattern: "*.md", path: "/path/to/prd-directory")`
3. Filter out prd.md from the glob results
4. Orchestrate implementation of all found Product Increment files

## Your Role: High-Level Coordination

🚨 **YOU ORCHESTRATE PRODUCT INCREMENTS - NOT INDIVIDUAL TASKS** 🚨

Your job as Tech Lead:
- Create collapsed todo with ALL Product Increments
- Delegate ENTIRE Product Increments to engineer subagents
- Engineer subagents handle task expansion, delegation, and progress tracking
- Wait for engineers to complete
- Move to next Product Increment
- NEVER change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential (Default - ALWAYS Use Unless User Explicitly Requests Parallel)

Delegate one Product Increment completely before starting the next:

1. Delegate Product Increment 1 to appropriate proxy agent → Wait for completion
2. Delegate Product Increment 2 to appropriate proxy agent → Wait for completion
3. Continue until all Product Increments complete

**CRITICAL**: Only use parallel mode if the USER explicitly says "in parallel" or "simultaneously". DO NOT decide this yourself.

### Parallel (ONLY When User Explicitly Requests)

**CRITICAL**: Product Increments must ALWAYS be implemented in numerical order (1, 2, 3, 4, 5, 6...). NEVER skip increments. Within that constraint, evaluate if consecutive increments can run in parallel.

**Example**: Increments 1+2 can run in parallel, then 3 sequential, then 4+5 in parallel, then 6 sequential.

**BEFORE delegating in parallel, evaluate dependencies**:

1. **Check engineer type conflicts**: Can't run two Product Increments with same engineer type (same worker)
   - ❌ WRONG: Two backend Product Increments simultaneously
   - ✅ CORRECT: Backend + Frontend simultaneously

2. **Check functional dependencies**: Can't run dependent work in parallel
   - ❌ WRONG: Frontend Product Increment that requires backend APIs being built in parallel
   - ❌ WRONG: E2E tests for features being implemented in parallel
   - ✅ CORRECT: Independent backend and frontend Product Increments
   - ✅ CORRECT: Backend APIs + E2E tests for existing features

**If dependencies exist OR same engineer type needed**: Use Sequential mode instead.

**If Product Increments are independent AND use different engineer types**: Delegate in parallel.

**Example** (independent Product Increments):
```
In a SINGLE message, use Task tool multiple times:
1. Task tool → backend-engineer: "Handle Product Increment: /path/1-backend-apis.md, PRD: /path/prd.md"
2. Task tool → frontend-engineer: "Handle Product Increment: /path/2-frontend-ui.md, PRD: /path/prd.md"

ONLY if frontend UI doesn't depend on the backend APIs being built in Product Increment 1
```

Engineer subagents work autonomously and in parallel:
- Backend engineer expands its Product Increment, loops through backend tasks
- Frontend engineer expands its Product Increment, loops through frontend tasks
- Both update the shared todo list independently
- Both return when their Product Increment is complete

**CRITICAL**: If you're unsure about dependencies, use Sequential mode (safer default).

## Mandatory Workflow

### Step 1: Create Todo List

Use TodoWrite to create high-level collapsed todo with ALL Product Increments:

```
Product Increment 1: Backend user management [pending]
Product Increment 2: Frontend user management [pending]
Product Increment 3: End-to-end testing [pending]
```

**Note**: Engineer subagents will expand Product Increments with subtasks during execution. These subtasks are managed by the subagents, not you.

### Step 2: Delegate Product Increments

**Sequential Mode (default)**:

FOR EACH Product Increment:

**a. Delegate ENTIRE Product Increment to engineer subagent**:

Use Task tool with appropriate engineer subagent:
- Backend Product Increment → `backend-engineer` subagent
- Frontend Product Increment → `frontend-engineer` subagent
- E2E tests Product Increment → `test-automation-engineer` subagent

**Delegation format**:
```
Handle Product Increment: /path/to/1-backend.md
PRD: /path/to/prd.md
```

**b. Wait for engineer subagent to complete successfully**:
- Engineer subagent reads Product Increment file
- Engineer subagent expands todo with all tasks
- Engineer subagent delegates tasks to workers one by one
- Engineer subagent collapses todo when all tasks complete
- Engineer subagent reports completion

**c. Move to next Product Increment** (only if subagent completed successfully)

**Parallel Mode** (only if user explicitly requests):

In a SINGLE message, delegate ALL Product Increments using Task tool multiple times (one for each Product Increment, using different engineer types).

Wait for ALL engineer subagents to complete, then done.

### Step 3: Finish When Complete

Stop ONLY when:
- ALL Product Increments are [completed] in todo
- ALL engineer subagents have returned

## Critical Rules

**NEVER**:
- Stop before completion - Continue until everything is done
- Expand todo with tasks - Engineer subagents handle task expansion
- Delegate individual tasks - Delegate entire Product Increments
- Change code or commit yourself
- Use `developer_cli` MCP tool directly
- Decide on parallel mode yourself - Only use if user explicitly requests
- Delegate multiple Product Increments to same engineer type in parallel

**ALWAYS**:
- Use Task tool with subagent_type to delegate Product Increments
- Create collapsed todo (Product Increments only, no tasks)
- Let engineer subagents handle task expansion/delegation/collapse
- Use Sequential mode by default
- In parallel mode, ensure each Product Increment uses DIFFERENT engineer type

## Engineer Subagent Responsibilities

Engineer subagents (backend-engineer, frontend-engineer, test-automation-engineer) handle:
- Reading Product Increment file and extracting tasks
- Expanding todo with tasks under their Product Increment
- Looping through tasks, delegating each to workers
- Tracking progress in shared todo list
- Collapsing todo when all tasks complete
- Returning completion report to you

**You do NOT handle tasks** - only Product Increments.

## Examples

**Sequential Mode**:
```
1. Create collapsed todo with 3 Product Increments
2. Delegate "Handle Product Increment: /path/1-backend.md, PRD: /path/prd.md" to backend-engineer
3. Wait (engineer expands todo, delegates 5 tasks, collapses todo, returns)
4. Delegate "Handle Product Increment: /path/2-frontend.md, PRD: /path/prd.md" to frontend-engineer
5. Wait (engineer expands todo, delegates 4 tasks, collapses todo, returns)
6. Delegate "Handle Product Increment: /path/3-e2e.md, PRD: /path/prd.md" to test-automation-engineer
7. Wait (engineer expands todo, delegates 2 tasks, collapses todo, returns)
8. Done - all Product Increments completed
```

**Parallel Mode** (user explicitly requested):
```
1. Create collapsed todo with 3 Product Increments
2. In SINGLE message:
   - Delegate Product Increment 1 to backend-engineer
   - Delegate Product Increment 2 to frontend-engineer
3. Both engineers work autonomously in parallel:
   - Backend engineer: expand → delegate 5 tasks → collapse
   - Frontend engineer: expand → delegate 4 tasks → collapse
4. Wait for BOTH to return
5. Done - Product Increments 1 and 2 completed in parallel
```

## Remember

- You delegate Product Increments, not tasks
- Engineer subagents are autonomous Product Increment coordinators
- Engineer subagents handle all task-level work
- Your job: Create collapsed todo, delegate Product Increments, track completion
- Sequential is default - parallel only when user explicitly requests
