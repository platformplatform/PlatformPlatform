---
description: Orchestrate implementation of a feature through task-level delegation to engineer subagents
argument-hint: [FeatureId] and/or [StoryIds] from [PRODUCT_MANAGEMENT_TOOL]
---

# Orchestrate Feature Implementation

[FeatureId] and/or [StoryIds]: $ARGUMENTS

## Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Load [feature] and [story] data** from `[PRODUCT_MANAGEMENT_TOOL]` using tool-specific IDs and methods from the guide above.

**Automatically determine if parallel execution is appropriate:**

Read the PRD and look for indicators that [stories] are designed for parallel work:
- PRD mentions "parallel" or "simultaneously" in Stories section
- [Story] descriptions mention "can work in parallel with" or "independent"
- [Story] descriptions mention "mocked dependencies" or "mocks"
- [Stories] are explicitly structured to suggest parallel execution

**Decision:**
- **If parallel indicators found**: Use Parallel Mode (inform user: "Detected parallel-optimized [stories]")
- **Otherwise**: Use Sequential Mode (default, safer - inform user: "Using sequential execution")

Continue with orchestrating implementation of all found [stories].

## Your Role: Task-Level Coordination

🚨 **YOU DELEGATE INDIVIDUAL TASKS - NOT STORIES** 🚨

Your job as Tech Lead:
- Load ALL [stories] and extract [tasks]
- Create expanded todo with ALL [stories] and ALL [tasks]
- Delegate individual [tasks] to engineer proxy agents
- Engineer proxy agents are pure passthroughs - they just forward your request to workers
- Track progress and mark [tasks] complete
- NEVER change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential Mode (Default)

Delegate one [task] completely before starting the next:

1. Delegate [task] 1 from [story] 1 → Wait for completion
2. Delegate [task] 2 from [story] 1 → Wait for completion
3. Continue with [story] 1 until all [tasks] complete
4. Move to [story] 2, delegate [task] 1 → Wait for completion
5. Continue until all [tasks] in all [stories] complete

### Parallel Mode

**CRITICAL**: [stories] must ALWAYS be implemented in the order they appear in [PRODUCT_MANAGEMENT_TOOL]. NEVER skip [stories]. Within that constraint, you can interleave [tasks] from multiple [stories].

**Example**: [task] 1 from [story] 1 + [task] 1 from [story] 2 simultaneously, then [task] 2 from [story] 1 + [task] 2 from [story] 2 simultaneously.

**BEFORE delegating in parallel, evaluate dependencies**:

1. **Check engineer type conflicts**: Can't run two tasks with same engineer type (same worker) in parallel
   - ❌ WRONG: Two backend tasks simultaneously
   - ✅ CORRECT: Backend task + Frontend task simultaneously

2. **Check functional dependencies**: Can't run dependent work in parallel
   - ❌ WRONG: Frontend task that requires backend API being built in that same parallel round
   - ❌ WRONG: E2E tests for features being implemented in that same parallel round
   - ✅ CORRECT: Independent backend and frontend tasks
   - ✅ CORRECT: Backend APIs + E2E tests for existing features

**If dependencies exist OR same engineer type needed**: Use Sequential mode instead.

**If tasks are independent AND use different engineer types**: Delegate in parallel.

**Example** (interleaving independent tasks):
```
In a SINGLE message, delegate multiple tasks:
1. backend-engineer: Feature: {featureId}, Story: {story1Id}, Task: {task1Id} - "Create user API endpoints"
2. frontend-engineer: Feature: {featureId}, Story: {story2Id}, Task: {task1Id} - "Create user management UI"

Wait for both to complete, then delegate next round:
3. backend-engineer: Feature: {featureId}, Story: {story1Id}, Task: {task2Id} - "Add validation"
4. frontend-engineer: Feature: {featureId}, Story: {story2Id}, Task: {task2Id} - "Add form validation"
```

**CRITICAL**: If you're unsure about dependencies, use Sequential mode (safer default).

## Mandatory Workflow

### Step 1: Load Stories and Tasks

For each [story] loaded in Mandatory Preparation, extract all [tasks].

Refer to `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` for tool-specific instructions on how to:
- Query for [tasks] within each [story]
- Extract [task] titles and IDs
- Determine [task] ordering

### Step 2: Create Expanded Todo List

Use TodoWrite to create fully expanded todo with ALL [stories] numbered and ALL [tasks] as subtasks:

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

**CRITICAL**: Ensure you have confirmed [taskId] values for ALL [tasks] before proceeding. [taskId] must be distinct from [storyId].

### Step 3: Delegate Tasks

**Sequential Mode (default)**:

FOR EACH [story]:
  **0. Update [story] status to [Active]** in [PRODUCT_MANAGEMENT_TOOL]:
    - [Story] is starting work
    - This signals that implementation has begun

  FOR EACH [task] in that [story]:
    **1. Mark [task] [in_progress]** in todo

    **2. Determine resetMemory value**:
    - First [task] of this [story]: `resetMemory=true` (clear context, start fresh)
    - Subsequent [tasks] of same [story]: `resetMemory=false` (continue accumulating context)

    **3. Delegate to engineer proxy agent**:

    Use Task tool with appropriate engineer subagent:
    - Backend [task] → `backend-engineer` subagent
    - Frontend [task] → `frontend-engineer` subagent
    - E2E test [task] → `qa-engineer` subagent
    - Branch: Use current git branch

    **Delegation format**:
    ```
    Feature: {featureId} ({featureTitle})
    Story: {storyId} ({storyTitle})
    Task: {taskId} ({taskTitle})
    Reset memory: {resetMemory}

    Please implement this [task].
    ```

    **4. Wait for engineer proxy to complete**:
    - Engineer proxy passes your exact request to worker
    - Worker implements, gets reviewed, commits
    - Engineer proxy returns response

    **5. Verify [task] completion**:
    - Check if response contains "✅ Task {taskId} completed successfully!"
    - **If SUCCESS marker found**:
      - Mark [task] [completed] in todo
      - Move to next [task]
    - **If NO success marker found ([task] FAILED)**:
      - Change [task] status to [Planned] in [PRODUCT_MANAGEMENT_TOOL]
      - Check git status for uncommitted changes
      - If uncommitted code exists: Stash with descriptive name (e.g., "{taskId}-failed-{sanitized-task-title}-{timestamp}")
      - Attempt to find alternative solution if possible
      - If [task] is blocking: Ask user for guidance
      - If [task] is non-blocking: Continue with other [tasks]

    **6. Move to next [task]**

**Parallel Mode** (only if user explicitly requests):

Work on multiple [stories] in parallel (each [story] uses a different engineer type). In each round, delegate one [task] from each [story] simultaneously, wait for all to return, then move to the next round. Example: If backend [story] has 4 [tasks] and frontend [story] has 6 [tasks], there will be 6 rounds - rounds 1-4 delegate to both engineers, rounds 5-6 delegate only to frontend-engineer.

FOR EACH round of parallel delegation:
  In a SINGLE message, delegate multiple [tasks] using Task tool multiple times

  Wait for ALL Task tool calls to return

  Verify each [task]:
  - Check if response contains "✅ Task {taskId} completed successfully!"
  - If success marker found: Mark [task] [completed] in todo
  - If no success marker found:
    - Change [task] status to [Planned] in [PRODUCT_MANAGEMENT_TOOL]
    - Check git status for uncommitted changes
    - If uncommitted code exists: Stash with descriptive name (e.g., "{taskId}-failed-{sanitized-task-title}-{timestamp}")
    - Attempt alternative solution if possible
    - If [task] is blocking: Continue work on other [stories] (user will be prompted later)
    - If [task] is non-blocking: Continue

  Continue with next round of parallel [tasks]

  For each [story], when its last [task] returns: Apply Step 4

### Step 4: Collapse Stories and Update Status

After the LAST [task] delegation in a [story] returns (succeeded or failed):

1. **Verify all [tasks] genuinely [completed]**:
   - Check that ALL [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are marked [completed]
   - **If any [task] is NOT [completed]**:
     - Evaluate if there are alternative approaches to complete the [tasks]
     - If no alternatives exist: Inform user about incomplete [tasks] and ask for guidance
     - DO NOT proceed with [story] status update
     - [story] remains [Active] until all [tasks] are genuinely [completed]

2. **If all [tasks] are [completed], update [story] status to [Review]** in [PRODUCT_MANAGEMENT_TOOL]:
   - All [tasks] are [completed]
   - [Story] is ready for final review
   - Status signals completion of implementation phase

3. Update todo:
   - Remove all subtask lines (├─ lines)
   - Keep only [story] line
   - Mark [story] [completed]

```
Story 1: Backend user management [completed]
Story 2: Frontend user management [in_progress]
├─ 2. Add form validation [in_progress]
Story 3: End-to-end testing [pending]
├─ 1. Create smoke tests [pending]
```

### Step 5: Finish When Complete

Stop ONLY when:
- ALL [stories] are [completed] in todo
- ALL [tasks] have been delegated and [completed]

## Critical Rules

**NEVER**:
- Stop before completion - Continue until everything is done
- Delegate entire [stories] - Delegate individual [tasks]
- Skip loading [stories] - Must load to extract [tasks]
- Keep todo collapsed - Must expand to show all [tasks]
- Change code or commit yourself
- Use `developer_cli` MCP tool directly
- Decide on parallel mode yourself - Only use if user explicitly requests
- Delegate multiple [tasks] to same engineer type in parallel

**ALWAYS**:
- Use Task tool with subagent_type to delegate [tasks]
- Create expanded todo ([stories] with all [task] subtasks)
- Load [stories] first to extract numbering and [tasks]
- Keep todo expanded until [story] is fully complete
- Use Sequential mode by default
- In parallel mode, ensure each [task] in a round uses DIFFERENT engineer type

## Engineer Proxy Agent Responsibilities

Engineer proxy agents (backend-engineer, frontend-engineer, qa-engineer) are PURE PASSTHROUGHS:
- They receive your delegation message
- They pass it VERBATIM to the worker via MCP
- They wait for worker to complete (implement + review + commit)
- They return worker's response to you

**Engineer proxies do NOT**:
- Expand/collapse todos
- Load data
- Make decisions
- Coordinate anything

**You handle ALL coordination** - loading data, tracking [tasks], managing todo.

## Examples

**Sequential Mode**:
```
1. Load all 3 [stories], extract 11 [tasks] total (with IDs)
2. Create expanded todo with 3 [stories] and 11 [tasks]
3. Update [Story] 1 status to [Active] in [PRODUCT_MANAGEMENT_TOOL]
4. Delegate [task] "1. Create user API endpoints" from [Story] 1 to backend-engineer (resetMemory=true)
5. Wait (proxy forwards to worker, worker implements+reviews+commits, proxy returns)
6. Verify response has "✅ Task completed successfully!" → Mark [task] 1 [completed]
7. Delegate [task] "2. Add validation logic" from [Story] 1 (resetMemory=false)
8. Wait, verify, and mark complete
9. Continue through all 5 [tasks] in [Story] 1 (verify each, resetMemory=false for all)
10. Verify all [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are [completed]
11. Update [Story] 1 status to [Review] in [PRODUCT_MANAGEMENT_TOOL]
12. Collapse [Story] 1 (remove subtasks, mark [completed])
13. Update [Story] 2 status to [Active] in [PRODUCT_MANAGEMENT_TOOL]
14. Delegate [task] "1. Create user management UI" from [Story] 2 to frontend-engineer (resetMemory=true)
15. Continue until all [stories] collapsed and [completed]
```

**Parallel Mode**:
```
1. Load all 3 [stories], extract 11 [tasks] total (with IDs)
2. Create expanded todo with 3 [stories] and 11 [tasks]
3. Update [Story] 1 and [Story] 2 status to [Active] in [PRODUCT_MANAGEMENT_TOOL]
4. Identify [tasks] that can run in parallel:
   - Round 1: [Story] 1 [Task] 1 (backend) + [Story] 2 [Task] 1 (frontend)
   - Round 2: [Story] 1 [Task] 2 (backend) + [Story] 2 [Task] 2 (frontend)
   - ...
5. In SINGLE message, delegate both [tasks] in Round 1 (both resetMemory=true):
   - Task tool → backend-engineer for [Story] 1 [Task] 1 (resetMemory=true)
   - Task tool → frontend-engineer for [Story] 2 [Task] 1 (resetMemory=true)
6. Wait for BOTH to complete
7. Verify each response has "✅ Task completed successfully!" → Mark [tasks] with success marker as [completed]
8. In SINGLE message, delegate both [tasks] in Round 2 (both resetMemory=false):
   - Task tool → backend-engineer for [Story] 1 [Task] 2 (resetMemory=false)
   - Task tool → frontend-engineer for [Story] 2 [Task] 2 (resetMemory=false)
9. Continue with next rounds until [stories] complete (verify each round, resetMemory=false for remaining [tasks])
10. Verify all [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are [completed]
11. Update [Story] 1 status to [Review], then [Story] 2 status to [Review] in [PRODUCT_MANAGEMENT_TOOL]
12. Collapse completed [stories]
```

## Remember

- You delegate [tasks], not [stories]
- Engineer proxies are passthroughs, not coordinators
- You manage the todo list, not the proxies
- Your job: Load data, expand todo, delegate [tasks], track completion
- Sequential is default - parallel only when user explicitly requests
- Keep todo expanded to show progress
