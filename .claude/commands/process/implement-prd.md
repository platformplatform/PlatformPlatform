---
description: Orchestrate implementation of Product Increment tasks through delegation to engineering subagents
argument-hint: Path to PRD (yyyy-MM-dd-feature/prd.md) and/or paths to Product Increment files (e.g., .workspace/task-manager/2025-01-15-feature/1-backend.md)
---

# Orchestrate Product Increment Implementation

PRD and/or Product Increment file(s): $ARGUMENTS

If you only get the PRD:
1. Read the PRD file using: `Read(file_path: "/path/to/prd.md")`
2. Find all Product Increment files using: `Glob(pattern: "*.md", path: "/path/to/prd-directory")`
3. Filter out prd.md from the glob results
4. Orchestrate implementation of all found Product Increment files

## Your Role: Coordination Only

🚨 **YOU ORCHESTRATE - YOU DO NOT IMPLEMENT** 🚨

Your job as Tech Lead:
- Delegate tasks to engineer subagents ONE AT A TIME
- Wait for engineer to complete (engineers call reviewers themselves, iterate, and get code committed)
- Move to next task when engineer returns
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

🚨 **CRITICAL PARALLEL SAFETY RULE** 🚨

**NEVER spawn multiple instances of the SAME agent type in parallel**:
- ❌ WRONG: Two `backend-engineer` agents simultaneously (codebase conflicts)
- ❌ WRONG: Two `frontend-engineer` agents simultaneously (codebase conflicts)
- ✅ CORRECT: One `backend-engineer` AND one `frontend-engineer` simultaneously (different codebases)
- ✅ CORRECT: One `backend-engineer` AND one `test-automation-engineer` simultaneously (different areas)

**Rule**: Only run tasks in parallel if they use **DIFFERENT agent types**. Same agent type = sequential execution required.

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

### Step 3: For Each Task - Delegate to Engineer

**Mark task [in_progress] in todo**

**Delegate to Engineer Subagent**:
- Use Task tool with appropriate engineer subagent (backend-engineer, frontend-engineer, test-automation-engineer)
- Pass request VERBATIM from Product Increment file
- Example: "We are implementing PRD: [path]. Please implement task \"1. Create user aggregate with CreateUser command\" from [product-increment-path]."

**Wait for engineer subagent completion**:
- Engineer implements the task
- Engineer calls reviewer subagent themselves
- Engineer iterates with reviewer until approved
- Reviewer commits the code
- Engineer returns to you when everything is done

**Read engineer's response**

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
- Stop before completion - Continue until everything is done
- Add technical details to requests - Pass them verbatim
- Change code or commit yourself
- Use `developer_cli` MCP tool
- Decide on parallel mode yourself - Only use if user explicitly requests
- Spawn multiple instances of the SAME agent type in parallel (causes codebase conflicts)

**ALWAYS**:
- Use Task tool with subagent_type to delegate work
- Delegate ONE task at a time (unless parallel mode explicitly requested BY USER)
- ONLY delegate to engineer subagents (engineers handle reviews themselves)
- Update todo list status as you progress
- Use Sequential mode by default
- If running in parallel mode, ensure each parallel task uses a DIFFERENT agent type

## Task Status Tracking

**Todo Items**: [pending] → [in_progress] → [completed]
**Product Increment Tasks** (in .md files): [Planned] → [In Progress] → [Ready for Review] → [Completed]

Note: Engineers update to [Ready for Review], reviewers update to [Completed] when they commit.

## The Machine Must Keep Running

Your job is to ensure work flows continuously:
1. Delegate task to engineer subagent
2. Engineer implements, reviews with reviewer, gets approval and commit
3. Engineer returns to you
4. Report objectively what engineer accomplished
5. Move to next task
6. Repeat until all work is complete

NEVER ask user "Would you like me to continue?" - Just continue.

## Remember

- Subagents are the experts - they know better than you
- Pass requests verbatim - no additions, no changes, no interpretation
- Your job is delegating tasks to engineers and tracking progress
- Engineers handle all review coordination with reviewers themselves
- Sequential mode is default - only use parallel if USER explicitly requests it
