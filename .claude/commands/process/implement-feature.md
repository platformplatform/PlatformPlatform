---
description: Orchestrate implementation of a feature through task-level delegation to engineer subagents
argument-hint: Feature ID and/or story IDs (e.g., "yyyy-MM-dd-feature/prd.md" or "1-backend.md" for Markdown, project/issue IDs for MCP tools)
---

# Orchestrate Feature Implementation

Feature ID and/or story ID(s): $ARGUMENTS

## Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Load feature and story data** from `[PRODUCT_MANAGEMENT_TOOL]` using tool-specific IDs and methods from the guide above.

**Automatically determine if parallel execution is appropriate:**

Read the PRD and look for indicators that stories are designed for parallel work:
- PRD mentions "parallel" or "simultaneously" in Stories section
- Story descriptions mention "can work in parallel with" or "independent"
- Story descriptions mention "mocked dependencies" or "mocks"
- Stories are explicitly structured to suggest parallel execution

**Decision:**
- **If parallel indicators found**: Use Parallel Mode (inform user: "Detected parallel-optimized stories")
- **Otherwise**: Use Sequential Mode (default, safer - inform user: "Using sequential execution")

Continue with orchestrating implementation of all found Story files.

## Your Role: Task-Level Coordination

🚨 **YOU DELEGATE INDIVIDUAL TASKS - NOT STORIES** 🚨

Your job as Tech Lead:
- Read ALL Story files and extract tasks
- Create expanded todo with ALL Stories and ALL tasks
- Delegate individual tasks to engineer proxy agents
- Engineer proxy agents are pure passthroughs - they just forward your request to workers
- Track progress and mark tasks complete
- NEVER change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential (Default - ALWAYS Use Unless User Explicitly Requests Parallel)

Delegate one task completely before starting the next:

1. Delegate Task 1 from Story 1 → Wait for completion
2. Delegate Task 2 from Story 1 → Wait for completion
3. Continue with Story 1 until all tasks complete
4. Move to Story 2, delegate Task 1 → Wait for completion
5. Continue until all tasks in all Stories complete

**CRITICAL**: Only use parallel mode if the USER explicitly says "in parallel" or "simultaneously". DO NOT decide this yourself.

### Parallel (ONLY When User Explicitly Requests)

**CRITICAL**: Stories must ALWAYS be implemented in numerical order (1, 2, 3, 4, 5, 6...). NEVER skip stories. Within that constraint, you can interleave tasks from multiple stories.

**Example**: Task 1 from Story 1 + Task 1 from Story 2 simultaneously, then Task 2 from Story 1 + Task 2 from Story 2 simultaneously.

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
In a SINGLE message, delegate multiple tasks:
1. backend-engineer: Feature: {featureId}, Story: {story1Id}, Task: {task1Id} - "Create user API endpoints"
2. frontend-engineer: Feature: {featureId}, Story: {story2Id}, Task: {task1Id} - "Create user management UI"

Wait for both to complete, then delegate next batch:
3. backend-engineer: Feature: {featureId}, Story: {story1Id}, Task: {task2Id} - "Add validation"
4. frontend-engineer: Feature: {featureId}, Story: {story2Id}, Task: {task2Id} - "Add form validation"
```

**CRITICAL**: If you're unsure about dependencies, use Sequential mode (safer default).

## Mandatory Workflow

### Step 1: Read Story Files

Read each Story file to extract:
- The file number (from filename: 1-backend.md, 2-frontend.md, 3-e2e-tests.md)
- The Story title (first heading in the file)
- ALL tasks listed in the file

### Step 2: Create Expanded Todo List

Use TodoWrite to create fully expanded todo with ALL Stories numbered and ALL tasks as subtasks:

```
Story 1: Backend user management [pending]
├─ 1. Create user API endpoints [pending]
├─ 2. Add validation logic [pending]
├─ 3. Implement user repository [pending]
Story 2: Frontend user management [pending]
├─ 1. Create user management UI [pending]
├─ 2. Add form validation [pending]
Story 3: End-to-end testing [pending]
├─ 1. Create smoke tests [pending]
```

**CRITICAL**: Keep this expanded format throughout execution so you and the user can track progress.

### Step 3: Delegate Tasks

**Sequential Mode (default)**:

FOR EACH Story (in numerical order):
  FOR EACH task in that Story:
    **a. Mark task [in_progress]** in todo

    **b. Delegate to engineer proxy agent**:

    Use Task tool with appropriate engineer subagent:
    - Backend task → `backend-engineer` subagent
    - Frontend task → `frontend-engineer` subagent
    - E2E test task → `test-automation-engineer` subagent

    **Delegation format**:
    ```
    Feature: {featureId} ({featureTitle})
    Story: {storyId} ({storyTitle})
    Task: {taskId} ({taskTitle})

    Please implement this task.
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

### Step 4: Collapse Stories as Complete

When ALL tasks in a Story are [completed]:
1. Remove all subtask lines (├─ lines)
2. Keep only Story line
3. Mark Story [completed]

```
Story 1: Backend user management [completed]
Story 2: Frontend user management [in_progress]
├─ 2. Add form validation [in_progress]
Story 3: End-to-end testing [pending]
├─ 1. Create smoke tests [pending]
```

### Step 5: Finish When Complete

Stop ONLY when:
- ALL Stories are [completed] in todo
- ALL tasks have been delegated and completed

## Critical Rules

**NEVER**:
- Stop before completion - Continue until everything is done
- Delegate entire Stories - Delegate individual tasks
- Skip reading Story files - Must read to extract tasks
- Keep todo collapsed - Must expand to show all tasks
- Change code or commit yourself
- Use `developer_cli` MCP tool directly
- Decide on parallel mode yourself - Only use if user explicitly requests
- Delegate multiple tasks to same engineer type in parallel

**ALWAYS**:
- Use Task tool with subagent_type to delegate tasks
- Create expanded todo (Stories with all task subtasks)
- Read Story files first to extract numbering and tasks
- Keep todo expanded until Story is fully complete
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
1. Read all 3 Story files, extract 11 tasks total
2. Create expanded todo with 3 Stories and 11 tasks
3. Delegate task "1. Create user API endpoints" from Story 1 to backend-engineer
4. Wait (proxy forwards to worker, worker implements+reviews+commits, proxy returns)
5. Mark task 1 complete, delegate task "2. Add validation logic" from Story 1
6. Wait and mark complete
7. Continue through all 5 tasks in Story 1
8. Collapse Story 1 (remove subtasks, mark [completed])
9. Start Story 2, delegate task "1. Create user management UI" to frontend-engineer
10. Continue until all Stories collapsed and [completed]
```

**Parallel Mode** (user explicitly requested):
```
1. Read all 3 Story files, extract 11 tasks total
2. Create expanded todo with 3 Stories and 11 tasks
3. Identify tasks that can run in parallel:
   - Batch 1: Story 1 Task 1 (backend) + Story 2 Task 1 (frontend)
   - Batch 2: Story 1 Task 2 (backend) + Story 2 Task 2 (frontend)
   - ...
4. In SINGLE message, delegate both tasks in Batch 1:
   - Task tool → backend-engineer for Story 1 Task 1
   - Task tool → frontend-engineer for Story 2 Task 1
5. Wait for BOTH to complete
6. Mark both tasks [completed]
7. In SINGLE message, delegate both tasks in Batch 2
8. Continue batching until Stories complete
9. Collapse completed Stories
```

## Remember

- You delegate tasks, not Stories
- Engineer proxies are passthroughs, not coordinators
- You manage the todo list, not the proxies
- Your job: Read files, expand todo, delegate tasks, track completion
- Sequential is default - parallel only when user explicitly requests
- Keep todo expanded to show progress
