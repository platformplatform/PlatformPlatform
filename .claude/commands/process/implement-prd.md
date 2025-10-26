---
description: Orchestrate implementation of all tasks in a PRD through task-level delegation to engineer subagents
argument-hint: Path to PRD (yyyy-MM-dd-feature/prd.md) and/or paths to Slice files (e.g., .workspace/task-manager/2025-01-15-feature/1-backend.md)
---

# Orchestrate PRD Implementation

PRD and/or Slice file(s): $ARGUMENTS

## Step 0: Read PRD and Determine Execution Mode

If you only get the PRD:
1. Read the PRD file using: `Read(file_path: "/path/to/prd.md")`
2. Find all Slice files using: `Glob(pattern: "*.md", path: "/path/to/prd-directory")`
3. Filter out prd.md from the glob results

**Automatically determine if parallel execution is appropriate:**

Read the PRD and look for indicators that slices are designed for parallel work:
- PRD mentions "parallel" or "simultaneously" in Slices section
- Slice descriptions mention "can work in parallel with" or "independent"
- Slice descriptions mention "mocked dependencies" or "mocks"
- Slices are explicitly structured to suggest parallel execution

**Decision:**
- **If parallel indicators found**: Use Parallel Mode (inform user: "Detected parallel-optimized slices")
- **Otherwise**: Use Sequential Mode (default, safer - inform user: "Using sequential execution")

Continue with orchestrating implementation of all found Slice files.

## Your Role: Task-Level Coordination

🚨 **YOU DELEGATE INDIVIDUAL TASKS - NOT PRODUCT INCREMENTS** 🚨

Your job as Tech Lead:
- Read ALL Slice files and extract tasks
- Create expanded todo with ALL Slices and ALL tasks
- Delegate individual tasks to engineer proxy agents
- Engineer proxy agents are pure passthroughs - they just forward your request to workers
- Track progress and mark tasks complete
- NEVER change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential (Default - ALWAYS Use Unless User Explicitly Requests Parallel)

Delegate one task completely before starting the next:

1. Delegate Task 1 from Slice 1 → Wait for completion
2. Delegate Task 2 from Slice 1 → Wait for completion
3. Continue with Slice 1 until all tasks complete
4. Move to Slice 2, delegate Task 1 → Wait for completion
5. Continue until all tasks in all Slices complete

**CRITICAL**: Only use parallel mode if the USER explicitly says "in parallel" or "simultaneously". DO NOT decide this yourself.

### Parallel (ONLY When User Explicitly Requests)

**CRITICAL**: Slices must ALWAYS be implemented in numerical order (1, 2, 3, 4, 5, 6...). NEVER skip increments. Within that constraint, you can interleave tasks from multiple Slices.

**Example**: Task 1 from PI 1 + Task 1 from PI 2 simultaneously, then Task 2 from PI 1 + Task 2 from PI 2 simultaneously.

**BEFORE delegating in parallel, evaluate dependencies**:

1. **Check engineer type conflicts**: Can't run two tasks with same engineer type (same worker) in parallel
   - ❌ WRONG: Two backend tasks simultaneously
   - ✅ CORRECT: Backend task + Frontend task simultaneously

2. **Check functional dependencies**: Can't run dependent work in parallel
   - ❌ WRONG: Frontend task that requires backend API being built in that same parallel batch
   - ❌ WRONG: E2E tests for features being implemented in that same parallel batch
   - ✅ CORRECT: Independent backend and frontend tasks
   - ✅ CORRECT: Backend APIs + E2E tests for existing features

**If dependencies exist OR same engineer type needed**: Use Sequential mode instead.

**If tasks are independent AND use different engineer types**: Delegate in parallel.

**Example** (interleaving independent tasks):
```
In a SINGLE message, use Task tool multiple times:
1. Task tool → backend-engineer: "We are implementing PRD: /path/prd.md. Please implement task \"1. Create user API endpoints\" from /path/1-backend.md."
2. Task tool → frontend-engineer: "We are implementing PRD: /path/prd.md. Please implement task \"1. Create user management UI\" from /path/2-frontend.md."

Wait for both to complete, then delegate next batch:
3. Task tool → backend-engineer: "We are implementing PRD: /path/prd.md. Please implement task \"2. Add validation\" from /path/1-backend.md."
4. Task tool → frontend-engineer: "We are implementing PRD: /path/prd.md. Please implement task \"2. Add form validation\" from /path/2-frontend.md."
```

**CRITICAL**: If you're unsure about dependencies, use Sequential mode (safer default).

## Mandatory Workflow

### Step 1: Read Slice Files

Read each Slice file to extract:
- The file number (from filename: 1-backend.md, 2-frontend.md, 3-e2e-tests.md)
- The Slice title (first heading in the file)
- ALL tasks listed in the file

### Step 2: Create Expanded Todo List

Use TodoWrite to create fully expanded todo with ALL Slices numbered and ALL tasks as subtasks:

```
Slice 1: Backend user management [pending]
├─ 1. Create user API endpoints [pending]
├─ 2. Add validation logic [pending]
├─ 3. Implement user repository [pending]
Slice 2: Frontend user management [pending]
├─ 1. Create user management UI [pending]
├─ 2. Add form validation [pending]
Slice 3: End-to-end testing [pending]
├─ 1. Create smoke tests [pending]
```

**CRITICAL**: Keep this expanded format throughout execution so you and the user can track progress.

### Step 3: Delegate Tasks

**Sequential Mode (default)**:

FOR EACH Slice (in numerical order):
  FOR EACH task in that Slice:
    **a. Mark task [in_progress]** in todo

    **b. Delegate to engineer proxy agent**:

    Use Task tool with appropriate engineer subagent:
    - Backend task → `backend-engineer` subagent
    - Frontend task → `frontend-engineer` subagent
    - E2E test task → `test-automation-engineer` subagent

    **Delegation format**:
    ```
    We are implementing PRD: /path/to/prd.md. Please implement task "[task number and description]" from /path/to/N-productincrement.md.
    ```

    **c. Wait for engineer proxy to complete**:
    - Engineer proxy passes your exact request to worker
    - Worker implements, gets reviewed, commits
    - Engineer proxy returns completion

    **d. Mark task [completed]** in todo

    **e. Move to next task**

**Parallel Mode** (only if user explicitly requests):

Group tasks by batches where each task in a batch uses DIFFERENT engineer type:

FOR EACH batch:
  In a SINGLE message, delegate ALL tasks in batch using Task tool multiple times

  Wait for ALL tasks in batch to complete

  Move to next batch

### Step 4: Collapse Slices as Complete

When ALL tasks in a Slice are [completed]:
1. Remove all subtask lines (├─ lines)
2. Keep only Slice line
3. Mark Slice [completed]

```
Slice 1: Backend user management [completed]
Slice 2: Frontend user management [in_progress]
├─ 2. Add form validation [in_progress]
Slice 3: End-to-end testing [pending]
├─ 1. Create smoke tests [pending]
```

### Step 5: Finish When Complete

Stop ONLY when:
- ALL Slices are [completed] in todo
- ALL tasks have been delegated and completed

## Critical Rules

**NEVER**:
- Stop before completion - Continue until everything is done
- Delegate entire Slices - Delegate individual tasks
- Skip reading Slice files - Must read to extract tasks
- Keep todo collapsed - Must expand to show all tasks
- Change code or commit yourself
- Use `developer_cli` MCP tool directly
- Decide on parallel mode yourself - Only use if user explicitly requests
- Delegate multiple tasks to same engineer type in parallel

**ALWAYS**:
- Use Task tool with subagent_type to delegate tasks
- Create expanded todo (Slices with all task subtasks)
- Read Slice files first to extract numbering and tasks
- Keep todo expanded until Slice is fully complete
- Use Sequential mode by default
- In parallel mode, ensure each task in a batch uses DIFFERENT engineer type

## Engineer Proxy Agent Responsibilities

Engineer proxy agents (backend-engineer, frontend-engineer, test-automation-engineer) are PURE PASSTHROUGHS:
- They receive your delegation message
- They pass it VERBATIM to the worker via MCP
- They wait for worker to complete (implement + review + commit)
- They return worker's response to you

**Engineer proxies do NOT**:
- Expand/collapse todos
- Read files
- Make decisions
- Coordinate anything

**You handle ALL coordination** - reading files, tracking tasks, managing todo.

## Examples

**Sequential Mode**:
```
1. Read all 3 Slice files, extract 11 tasks total
2. Create expanded todo with 3 Slices and 11 tasks
3. Delegate task "1. Create user API endpoints" from PI 1 to backend-engineer
4. Wait (proxy forwards to worker, worker implements+reviews+commits, proxy returns)
5. Mark task 1 complete, delegate task "2. Add validation logic" from PI 1
6. Wait and mark complete
7. Continue through all 5 tasks in PI 1
8. Collapse PI 1 (remove subtasks, mark [completed])
9. Start PI 2, delegate task "1. Create user management UI" to frontend-engineer
10. Continue until all Slices collapsed and [completed]
```

**Parallel Mode** (user explicitly requested):
```
1. Read all 3 Slice files, extract 11 tasks total
2. Create expanded todo with 3 Slices and 11 tasks
3. Identify tasks that can run in parallel:
   - Batch 1: PI 1 Task 1 (backend) + PI 2 Task 1 (frontend)
   - Batch 2: PI 1 Task 2 (backend) + PI 2 Task 2 (frontend)
   - ...
4. In SINGLE message, delegate both tasks in Batch 1:
   - Task tool → backend-engineer for PI 1 Task 1
   - Task tool → frontend-engineer for PI 2 Task 1
5. Wait for BOTH to complete
6. Mark both tasks [completed]
7. In SINGLE message, delegate both tasks in Batch 2
8. Continue batching until Slices complete
9. Collapse completed Slices
```

## Remember

- You delegate tasks, not Slices
- Engineer proxies are passthroughs, not coordinators
- You manage the todo list, not the proxies
- Your job: Read files, expand todo, delegate tasks, track completion
- Sequential is default - parallel only when user explicitly requests
- Keep todo expanded to show progress
