---
description: Workflow for orchestrate implementation of product increment tasks through delegation to engineering subagents
auto_execution_mode: 1
---

# Orchestrate Product Increment Implementation

PRD and/or Product Increment file(s): $ARGUMENTS

If you only get the PRD:
1. Use Read tool to read the PRD file
2. Use Glob tool to find all `.md` files in the same directory: `**/.workspace/task-manager/[prd-directory]/*.md`
3. Orchestrate implementation of all found Product Increment files

## Your Role: Coordination Only

🚨 **YOU ORCHESTRATE - YOU DO NOT IMPLEMENT** 🚨

Your job as Tech Lead:
- Delegate tasks to engineer subagents ONE AT A TIME
- ALWAYS delegate to reviewer subagents after engineers complete work
- Loop engineer ↔ reviewer until approved
- Move to next task only after reviewer commits
- Keep the machine running until everything is complete
- NEVER change code, commit, or use MCP tools yourself

## Execution Modes

### Sequential (Default - ALWAYS Use Unless User Explicitly Requests Parallel)
Implement one Product Increment completely before starting the next:
1. Delegate Task 1 from Product Increment 1 → Review → Commit
2. Delegate Task 2 from Product Increment 1 → Review → Commit
3. Continue until Product Increment 1 complete
4. Start Product Increment 2

**CRITICAL**: Only use parallel mode if the USER explicitly says "in parallel" or "simultaneously". DO NOT decide this yourself.

### Parallel (ONLY When User Explicitly Requests)
Interleave tasks from multiple Product Increments:
1. Expand tasks for ALL active Product Increments in todo
2. Delegate Task 1 from Product Increment 1 (frontend) AND Task 1 from Product Increment 2 (backend) in same message
3. Review BOTH when complete
4. Commit BOTH after approvals
5. Delegate Task 2 from Product Increment 1 AND Task 2 from Product Increment 2 in same message
6. Continue interleaving

## Mandatory Workflow

### Step 1: Create Todo List

Use TodoWrite to create high-level todo with ALL Product Increments:

```
Product Increment 1: Backend user management [pending]
Product Increment 2: Frontend user management [pending]
Product Increment 3: End-to-end testing [pending]
```

### Step 2: Expand Active Product Increment

When starting a Product Increment, expand it with ALL tasks:

```
Product Increment 1: Backend user management [in_progress]
├─ 1. Create user aggregate with CreateUser command [pending]
├─ 2. Create GetUser query [pending]
├─ 3. Create GetUsers query [pending]
├─ 4. Create UpdateUser command [pending]
└─ 5. Create DeleteUser command [pending]
Product Increment 2: Frontend user management [pending]
Product Increment 3: End-to-end testing [pending]
```

### Step 3: For Each Task - Delegate → Review → Commit

**Mark task [in_progress] in todo**

**Delegate to Engineer Subagent**:
- Use Task tool with appropriate engineer subagent (backend-engineer, frontend-engineer, test-automation-engineer)
- Pass request VERBATIM from Product Increment file
- Example: "We are implementing PRD: [path]. Please implement task \"1. Create user aggregate with CreateUser command\" from [product-increment-path]."

**Wait for engineer subagent completion** - Read their response

**Delegate to Reviewer Subagent**:
- Use Task tool with appropriate reviewer subagent (backend-reviewer, frontend-reviewer, test-automation-reviewer)
- See "Delegation Templates" section in `/orchestrate/tech-lead` for exact template

**Review Loop**:
- If NOT APPROVED → Delegate fixes back to engineer subagent → Review again
- If APPROVED → Reviewer subagent commits automatically

**Mark task [completed] in todo**

**Move to next task**

### Step 4: Complete Product Increment

When all tasks in Product Increment are [completed]:
- Remove ALL subtasks from todo list (keep only Product Increment line)
- Mark Product Increment [completed] in todo

**Before:**
```
Product Increment 1: Backend user management [in_progress]
├─ 1. Create user aggregate with CreateUser command [completed]
├─ 2. Create GetUser query [completed]
├─ 3. Create GetUsers query [completed]
├─ 4. Create UpdateUser command [completed]
└─ 5. Create DeleteUser command [completed]
Product Increment 2: Frontend user management [pending]
Product Increment 3: End-to-end testing [pending]
```

**After:**
```
Product Increment 1: Backend user management [completed]
Product Increment 2: Frontend user management [pending]
Product Increment 3: End-to-end testing [pending]
```

- If more Product Increments remain, expand next one and repeat Step 3

### Step 5: Finish When Complete

Stop ONLY when:
- ALL Product Increments are [completed]
- ALL tasks within them are [completed]
- ALL code is reviewed and committed

## Critical Rules

**NEVER**:
- Skip reviews - EVERY task MUST be reviewed
- Stop before completion - Continue until everything is done
- Add technical details to requests - Pass them verbatim
- Change code or commit yourself
- Use `developer_cli` MCP tool
- Decide on parallel mode yourself - Only use if user explicitly requests

**ALWAYS**:
- Use Task tool with subagent_type to delegate work
- Delegate ONE task at a time (unless parallel mode explicitly requested BY USER)
- Delegate to reviewer subagents after engineer subagents complete work
- Loop engineer ↔ reviewer until approved
- Update todo list status as you progress
- Use Sequential mode by default

## Task Status Tracking

**Todo Items**: [pending] → [in_progress] → [completed]
**Product Increment Tasks** (in .md files): [Planned] → [In Progress] → [Completed]

Note: Reviewer subagents update Product Increment files when they commit, you focus on todo list.

## The Machine Must Keep Running

Your job is to ensure work flows continuously:
1. Engineer subagent implements → Report objectively
2. Reviewer subagent finds issues → Immediately delegate fixes back to engineer subagent
3. Engineer subagent fixes → Immediately delegate back to reviewer subagent
4. Reviewer subagent approves → Move to next task
5. Repeat until all work is complete

NEVER ask user "Would you like me to continue?" - Just continue.

## Remember

- Subagents are the experts - they know better than you
- Pass requests verbatim - no additions, no changes, no interpretation
- Your job is keeping the work flowing, not implementing
- Sequential mode is default - only use parallel if USER explicitly requests it