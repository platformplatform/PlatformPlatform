---
description: Orchestrate implementation of a feature through task-level delegation to engineer subagents
argument-hint: [FeatureId] from [PRODUCT_MANAGEMENT_TOOL] (optional)
---

# Orchestrate Feature Implementation

[FeatureId] (optional): $ARGUMENTS

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode*.

- **Agentic mode**: You run autonomously without human supervision - work with your team to find solutions. The [FeatureId] may be provided as argument, or you ask the user which feature to implement.
- **Standalone mode**: The user guides you interactively. Ask questions and collaborate with the user throughout the feature implementation.

## STEP 0: Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Select feature to implement**:

   **If [FeatureId] provided as argument:** Use the provided [FeatureId].

   **If NO [FeatureId] provided:** Ask user which feature to implement, or offer to list available features.

   - **Ask user**: "Which feature would you like to implement? (Provide feature ID/name, or I can list available features)"
   - **If user requests list**: Query [PRODUCT_MANAGEMENT_TOOL] for:
     - Recently created features (last 48 hours)
     - All features in [Planned] status
     - Show: Feature ID, name, description (first line), created date
   - User provides feature ID (e.g., "proj_abc123" or "PP-100")
   - Validate feature exists in [PRODUCT_MANAGEMENT_TOOL]
   - If not found, ask again or offer to list features

3. **Load [feature] and [task] data** from `[PRODUCT_MANAGEMENT_TOOL]` using the selected/provided [FeatureId].

4. **Automatically determine if parallel execution is appropriate**:

   Read the PRD and look for indicators that [tasks] are designed for parallel work:
   - PRD mentions "parallel" or "simultaneously" in Tasks section
   - [Task] descriptions mention "can work in parallel with" or "independent"
   - [Task] descriptions mention "mocked dependencies" or "mocks"
   - [Tasks] are explicitly structured to suggest parallel execution

   **Decision:**
   - **If parallel indicators found**: Use Parallel Mode (inform user: "Detected parallel-optimized [tasks]")
   - **Otherwise**: Use Sequential Mode (default, safer—inform user: "Using sequential execution")

5. **Create Todo List**

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Load all [tasks] from the [feature]", "status": "pending", "activeForm": "Loading tasks"},
    {"content": "Update [feature] status to [Active]", "status": "pending", "activeForm": "Updating feature status"},
    {"content": "Delegate [tasks] to engineers and track completion", "status": "pending", "activeForm": "Delegating tasks"},
    {"content": "Update [feature] status to [Resolved]", "status": "pending", "activeForm": "Updating feature status to Resolved"}
  ]
}
```

**Note**: After creating this base todo, you'll replace "Delegate [tasks] to engineers" with actual [task] items from the [feature] (see Step 2 below).

---

## Your Role: Task-Level Coordination

**You delegate tasks to engineers**

Your job as Coordinator:
- Load ALL [tasks] from the [feature]
- Create todo list with ALL [tasks]
- Delegate [tasks] to engineer proxy agents
- Engineer proxy agents are pure passthroughs—they just forward your request to workers
- Track progress and mark [tasks] complete
- Don't change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential Mode (Default)

Delegate one [task] completely before starting the next:

1. Delegate [task] 1 from [feature] → Wait for completion
2. Delegate [task] 2 from [feature] → Wait for completion
3. Continue until all [tasks] in [feature] complete

### Parallel Mode

[tasks] must be implemented in the order they appear in [PRODUCT_MANAGEMENT_TOOL]. Don't skip [tasks]. Within that constraint, you can run independent [tasks] in parallel.

**Example**: Backend [task] + Frontend [task] simultaneously (if independent)

**BEFORE delegating in parallel, evaluate dependencies**:

1. **Check engineer type conflicts**: Can't run two tasks with same engineer type (same worker) in parallel
   - ❌ WRONG: Two backend tasks simultaneously
   - ✅ CORRECT: Backend task + Frontend task simultaneously

2. **Check functional dependencies**: Can't run dependent work in parallel
   - ❌ WRONG: Frontend task that requires backend API being built in that same parallel round
   - ❌ WRONG: E2E tests for features being implemented in that same parallel round
   - ✅ CORRECT: Independent backend and frontend tasks
   - ✅ CORRECT: Backend APIs + E2E tests for existing features

**If dependencies exist OR same engineer type needed**: Use Sequential mode instead

**If tasks are independent AND use different engineer types**: Delegate in parallel

**Example** (parallel independent tasks):
```
In a SINGLE message, delegate multiple tasks:
1. backend-engineer: Feature: {featureId}, Task: {task1Id} - "Backend for user CRUD operations"
2. frontend-engineer: Feature: {featureId}, Task: {task2Id} - "Frontend UI skeleton for user management"

Wait for both to complete, then delegate next round (sequential):
3. frontend-engineer: Feature: {featureId}, Task: {task3Id} - "Connect frontend to backend"

Then continue with next parallel round if more independent tasks exist.
```

If you're unsure about dependencies, use Sequential mode (safer default)

## Mandatory Workflow

**Note:** If you receive MCP errors about agents not running, inform the user to start the required agents (backend-engineer, frontend-engineer, qa-engineer) in separate terminals

### Step 1: Load Tasks

Load all [tasks] from the [feature] loaded in Mandatory Preparation

Refer to `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` for tool-specific instructions on how to:
- Query for [tasks] within the [feature]
- Extract [task] titles and IDs
- Determine [task] ordering

### Step 2: Create Todo List

Use TodoWrite to create todo list with ALL [tasks]:

```
1. Backend for user CRUD operations [pending]
2. Frontend UI skeleton for user management [pending]
3. Connect frontend to backend [pending]
4. End-to-end tests for user management [pending]
```

Ensure you have confirmed [taskId] values for all [tasks] before proceeding

### Step 3: Delegate Tasks

**Sequential Mode (default)**:

**0. Update [feature] status to [Active]** in [PRODUCT_MANAGEMENT_TOOL] (once at start)

FOR EACH [task]:
  **1. Mark [task] [in_progress]** in todo

  **2. Determine resetMemory value**:
  - First delegation of a [task]: `resetMemory=true` (start fresh)
  - Re-delegation for follow-up/fix: `resetMemory=false` (maintain context)

  **3. Delegate to engineer proxy agent**:

  Use Task tool with appropriate engineer subagent:
  - Backend [task] → `backend-engineer` subagent
  - Frontend [task] → `frontend-engineer` subagent
  - E2E test [task] → `qa-engineer` subagent

  **Delegation format** (include all parameters in the prompt):
  ```
  Feature: {featureId} ({featureTitle})
  Task: {taskId} ({taskTitle})
  Branch: {currentBranch}
  Reset memory: true

  Please implement this [task].
  ```

  The proxy agent will parse this and call the MCP start_worker_agent tool with these parameters

  **4. Wait for engineer proxy to complete**:
  - Engineer proxy passes your exact request to worker
  - Worker implements, gets reviewed, commits
  - Engineer proxy returns response

  **5. Verify [task] completion**:
  - Check if response contains "✅ Task {taskId} completed successfully!"
  - **If SUCCESS marker found**:
    - Verify code was committed by checking recent commits
    - Verify [task] marked [Completed] in [PRODUCT_MANAGEMENT_TOOL]
    - **If backend [task]**: Restart Aspire AppHost using the watch MCP tool to apply database migrations and backend changes
    - **If anything unexpected (multiple [tasks] done, uncommitted code, failing tests, etc.)**:
      - Zero tolerance - system started clean, any warnings or errors means we broke it and must be fixed before continuing (follow the Boy Scout rule)
      - Stop immediately, diagnose the problem, and make a plan to get back on track
      - Delegate fixes to engineers - don't fix anything yourself
      - **If you need to re-delegate to the same engineer for follow-up**: Use resetMemory=false to maintain context
      - In edge cases, revert commits and reset [PRODUCT_MANAGEMENT_TOOL] state to start over
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

Work on multiple [tasks] in parallel (each [task] uses a different engineer type). In each round, delegate independent [tasks] simultaneously, wait for all to return, then move to the next round.

**Delegation format for parallel mode** (include all parameters in the prompt):
```
Feature: {featureId} ({featureTitle})
Task: {taskId} ({taskTitle})
Branch: {currentBranch}
Reset memory: true

⚠️ Parallel Work: You are working in parallel with {other-engineer} on {other-task-title}. You may see their git commits. If you encounter errors that seem related to their changes, sleep 5-10 minutes and re-test.

Please implement this [task].
```

The proxy agent will parse this and call the MCP start_worker_agent tool with these parameters

FOR EACH round of parallel delegation:
  In a SINGLE message, delegate multiple [tasks] using Task tool multiple times

  Wait for ALL Task tool calls to return

  Verify each [task]:
  - Check if response contains "✅ Task {taskId} completed successfully!"
  - If success marker found:
    - Verify code was committed by checking recent commits
    - Verify [task] marked [Completed] in [PRODUCT_MANAGEMENT_TOOL]
    - **If backend [task]**: Restart Aspire AppHost using the watch MCP tool to apply database migrations and backend changes
    - **If anything unexpected (multiple [tasks] done, uncommitted code, failing tests, etc.)**:
      - Zero tolerance - system started clean, any warnings or errors means we broke it and must be fixed before continuing (follow the Boy Scout rule)
      - Stop immediately, diagnose the problem, and make a plan to get back on track
      - Delegate fixes to engineers - don't fix anything yourself
      - **If you need to re-delegate to the same engineer for follow-up**: Use resetMemory=false to maintain context
      - In edge cases, revert commits and reset [PRODUCT_MANAGEMENT_TOOL] state to start over
    - Mark [task] [completed] in todo
  - If no success marker found:
    - Change [task] status to [Planned] in [PRODUCT_MANAGEMENT_TOOL]
    - Check git status for uncommitted changes
    - If uncommitted code exists: Stash with descriptive name (e.g., "{taskId}-failed-{sanitized-task-title}-{timestamp}")
    - Attempt alternative solution if possible
    - If [task] is blocking: Ask user for guidance
    - If [task] is non-blocking: Continue with other [tasks]

  Continue with next round of parallel [tasks]

### Step 4: Update Feature Status

After ALL [tasks] are completed:

1. **Verify all [tasks] genuinely [completed]**:
   - Check that ALL [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are marked [completed]
   - **If any [task] is NOT [completed]**:
     - Evaluate if there are alternative approaches to complete the [tasks]
     - If no alternatives exist: Inform user about incomplete [tasks] and ask for guidance
     - DO NOT proceed with [feature] status update

2. **If all [tasks] are [completed], update [feature] status to [Resolved]** in [PRODUCT_MANAGEMENT_TOOL]:
   - All [tasks] are [completed]
   - [Feature] implementation is complete
   - Status signals completion of implementation phase (not deployed yet)

### Step 5: Finish When Complete

Stop ONLY when:
- ALL [tasks] are [completed] in todo
- ALL [tasks] have been delegated and [completed]
- [Feature] status is [Resolved]

## Rules

**Don't**:
- Stop before completion—continue until everything is done
- Change code or commit yourself
- Use `developer_cli` MCP tool directly
- Decide on parallel mode yourself—only use if user explicitly requests
- Delegate multiple [tasks] to same engineer type in parallel

**Do**:
- Use Task tool with subagent_type to delegate [tasks]
- Load all [tasks] from [feature]
- Create simple todo list with [tasks]
- Use Sequential mode by default
- In parallel mode, ensure each [task] in a round uses different engineer type
- Use resetMemory=true for first delegation, resetMemory=false for follow-ups on same task

## Engineer Proxy Agent Responsibilities

Engineer proxy agents (backend-engineer, frontend-engineer, qa-engineer) are PURE PASSTHROUGHS:
- They receive your delegation message
- They pass it VERBATIM to the worker via MCP
- They wait for worker to complete (implement + review + commit)
- They return worker's response to you

**Engineer proxies do NOT**:
- Load data
- Make decisions
- Coordinate anything

**You handle ALL coordination**—loading data, tracking [tasks], managing todo

## Examples

**Sequential Mode**:
```
1. Load [feature] and all 3 [tasks]
2. Create todo with 3 [tasks]
3. Update [Feature] status to [Active] in [PRODUCT_MANAGEMENT_TOOL]
4. Delegate using Task tool (backend-engineer) with prompt:
   "Feature: feature-id-123 (User management)
    Task: task-id-001 (Backend for user CRUD operations)
    Branch: feature/user-management
    Reset memory: true

    Please implement this [task]."
5. Wait (proxy forwards to worker, worker implements+reviews+commits, proxy returns)
6. Verify response has "✅ Task completed successfully!" → Mark [task] [completed]
7. Delegate using Task tool (frontend-engineer) with similar prompt format
8. Wait, verify, and mark complete
9. Delegate using Task tool (qa-engineer) with similar prompt format
10. Wait, verify, and mark complete
11. Verify all [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are [completed]
12. Update [Feature] status to [Resolved] in [PRODUCT_MANAGEMENT_TOOL]
13. Done!
```

**Parallel Mode**:
```
1. Load [feature] and all 4 [tasks]
2. Create todo with 4 [tasks]
3. Update [Feature] status to [Active] in [PRODUCT_MANAGEMENT_TOOL]
4. Identify [tasks] that can run in parallel:
   - Round 1: Frontend UI skeleton (frontend) + Backend CRUD (backend) - parallel
   - Round 2: Connect frontend to backend (frontend) - sequential after round 1
   - Round 3: E2E tests (qa) - sequential after round 2
5. In SINGLE message, delegate both [tasks] in Round 1 using Task tool:

   Task tool → frontend-engineer with prompt:
   "Feature: feature-id-123 (User management)
    Task: task-id-002 (Frontend UI skeleton for user management)
    Branch: feature/user-management
    Reset memory: true

    ⚠️ Parallel Work: You are working in parallel with backend-engineer on Backend CRUD. You may see their commits.

    Please implement this [task]."

   Task tool → backend-engineer with prompt:
   "Feature: feature-id-123 (User management)
    Task: task-id-001 (Backend for user CRUD operations)
    Branch: feature/user-management
    Reset memory: true

    ⚠️ Parallel Work: You are working in parallel with frontend-engineer on Frontend UI skeleton. You may see their commits.

    Please implement this [task]."

6. Wait for BOTH to complete
7. Verify each response has "✅ Task completed successfully!" → Mark both [tasks] [completed]
8. Delegate Task tool (frontend-engineer) with prompt including Feature/Task/Title/Branch
9. Wait, verify, mark complete
10. Delegate Task tool (qa-engineer) with prompt including Feature/Task/Title/Branch
11. Wait, verify, mark complete
12. Verify all [tasks] in todo AND [PRODUCT_MANAGEMENT_TOOL] are [completed]
13. Update [Feature] status to [Resolved] in [PRODUCT_MANAGEMENT_TOOL]
14. Done!
```

## Remember

- You delegate entire [tasks] (large scope—complete vertical slices)
- Engineer proxies are passthroughs, not coordinators
- You manage the todo list, not the proxies
- Your job: Load [tasks] from [feature], create todo, delegate [tasks], track completion
- Sequential is default—parallel only when user explicitly requests
- Use resetMemory=true for first delegation of each [task], resetMemory=false for re-delegations
