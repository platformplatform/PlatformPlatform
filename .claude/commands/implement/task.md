---
description: Implement a specific task from a Product Increment following the systematic workflow
args:
  - name: title
    description: Task title to implement (e.g., "Add user filtering")
    required: false
---

# Implement Task Workflow

You are implementing: **{{{title}}}**

## STEP 0: Read Task Assignment

**Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
- `requestFilePath`: Full path to your request file
- `prdPath`: Path to PRD (if Product Increment task)
- `productIncrementPath`: Path to Product Increment (if applicable)
- `taskNumberInIncrement`: Your task number in the increment (if applicable)
- `title`: Task title

**Then read the request file** from the path in `requestFilePath`.

**If `prdPath` exists in current-task.json:**
1. Read PRD from the path in `prdPath`
2. Read Product Increment plan from the path in `productIncrementPath`
3. Understand your task (`taskNumberInIncrement`) within the larger feature context
4. **Mark your task as `[In Progress]`**: Edit the Product Increment file and change your task status from `[Pending]` to `[In Progress]`

**CRITICAL - Verify Previous Work Committed**:

Before proceeding, verify your previous task was committed:
1. Run `git log --oneline -5` to check recent commits
2. Look for commits containing your agent type (e.g., "backend-engineer", "frontend-engineer")
3. If your previous task is uncommitted: **REFUSE to start** and respond with error explaining uncommitted work exists
4. Note: Changes from other engineers (parallel work) are expected and fine - only verify YOUR previous work is committed

---

## CRITICAL - Autonomous Operation

You run WITHOUT human supervision. NEVER ask for guidance or refuse to do work. Work with our team to find a solution.

**Token limits approaching?** Use `/compact` strategically (e.g., after being assigned a new task, but before reading task assignment, before catching up).

---

## STEP 1: Create Todo List - DO THIS NOW!

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Understand context and catch up efficiently", "status": "pending", "activeForm": "Understanding context and catching up"},
    {"content": "Study relevant rules for the task at hand", "status": "pending", "activeForm": "Studying relevant rules"},
    {"content": "Research existing patterns for this task type", "status": "pending", "activeForm": "Researching existing patterns"},
    {"content": "Implement task [name of the task from request file]", "status": "pending", "activeForm": "Implementing task"},
    {"content": "Validate implementation builds and fix all static code analysis warnings", "status": "pending", "activeForm": "Validating implementation"},
    {"content": "Validate translations (frontend tasks only)", "status": "pending", "activeForm": "Validating translations"},
    {"content": "Mark task as Ready for Review", "status": "pending", "activeForm": "Marking task as Ready for Review"},
    {"content": "Call reviewer subagent to review and commit your code", "status": "pending", "activeForm": "Calling reviewer subagent"},
    {"content": "Report any workflow errors encountered (wrong paths, missing tools, etc.)", "status": "pending", "activeForm": "Reporting workflow errors"},
    {"content": "Critically evaluate remaining tasks and update plan", "status": "pending", "activeForm": "Evaluating remaining tasks"}
  ]
}
```

After creating base todo, expand "Implement task" with subtasks from Product Increment file (if applicable).

---

## Workflow Steps

**STEP 2**: Study rules (`.claude/rules/backend/` or `.claude/rules/frontend/`)

**STEP 3**: Research similar implementations in codebase

**STEP 4**: Implement each subtask, use **build** and **test** MCP tools

**STEP 5**: Run validation tools in parallel (backend tasks only)

For **backend tasks**, run **test** and **inspect** in parallel using the Task tool:
- Spawn two `backend-tool-runner` subagents simultaneously
- One runs `test`, the other runs `inspect`
- Wait for both to complete
- Fix any failures or warnings (both must pass)

**Parallel execution example**:
```
In a single message, use Task tool twice:
1. Task tool → backend-tool-runner: "Run backend tool: test"
2. Task tool → backend-tool-runner: "Run backend tool: inspect"
```

For **frontend tasks**, use **test** and **inspect** MCP tools directly.

**STEP 6**: Frontend only - validate translations

**STEP 7**: Edit Product Increment file: `[In Progress]` → `[Ready for Review]`

**STEP 8**: Delegate to reviewer subagent to review and commit your code

**CRITICAL - Before calling reviewer**:

1. Run `git status --porcelain` to see ALL changed files
2. Identify YOUR files (files you created/modified for THIS task):
   - Backend: Include *.Api.json files (even though in WebApp folder - generated from your API changes)
   - Frontend: Exclude *.Api.json files (these belong to backend, not you)
   - Don't forget .po translation files
   - Exclude files from parallel engineers (different agent types)
   - If you changed files outside your scope: `git restore <file>` to revert
3. List YOUR files in "Files Changed" section (one per line with status)

**Delegation format**:
```
[One short sentence: what you implemented or fixed]

## Files Changed
- path/to/file1.tsx
- path/to/file2.cs
- path/to/translations.po

Request: [path from current-task.json: requestFilePath]
Response: [response file path]
```

**Review loop**:
- If reviewer returns NOT APPROVED → Fix issues → Call reviewer subagent again
- If reviewer returns APPROVED → Check YOUR files (not parallel engineers' files) are committed → Proceed to completion
- **NEVER call CompleteWork unless reviewer approved and committed your code**
- **NEVER commit code yourself** - only the reviewer commits
- ⚠️ **If rejected 3+ times with same feedback despite validation tools passing:** Report problem with severity: error, then STOP COMPLETELY. No workarounds, no proceeding, no commits - just STOP and wait for human intervention.

**STEP 9**: Re-read Product Increment, update plan if needed

**STEP 10**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

After completing all work AND receiving reviewer approval, you MUST call the MCP **CompleteWork** tool with `mode: "task"` to signal completion. This tool call will IMMEDIATELY TERMINATE your session - there is no going back after this call.

ALWAYS call CompleteWork after reviewer approval, even if this is the last task in a Product Increment.

**Before calling CompleteWork**:
1. Ensure all work is complete and all todos are marked as completed
2. Write a comprehensive response (what you accomplished, notes for Tech Lead)
3. Create an objective technical summary in sentence case (like a commit message)

**Call MCP CompleteWork tool**:
- `mode`: "task"
- `agentType`: Your agent type (backend-engineer, frontend-engineer, or test-automation-engineer)
- `taskSummary`: Objective technical description of what was implemented (imperative mood, sentence case). Examples: "Add team member endpoints with authorization", "Implement user avatar upload", "Fix null reference in payment processor". NEVER use subjective evaluations like "Excellent implementation" or "Clean code".
- `responseContent`: Your full response in markdown

⚠️ Your session terminates IMMEDIATELY after calling CompleteWork

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy the JSON from STEP 0**

**❌ DON'T: Create custom todo format**
