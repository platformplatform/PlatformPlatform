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

**Read `current-task.json` in your workspace root** to get:
- `request_file_path`: Full path to your request file
- `prd_path`: Path to PRD (if Product Increment task)
- `product_increment_path`: Path to Product Increment (if applicable)
- `task_number_in_increment`: Your task number in the increment (if applicable)
- `title`: Task title

**Then read the request file** from the path in `request_file_path`.

**Request file contains**:
- Task description/instructions
- Any additional context from Tech Lead

If this is a Product Increment task, also read the PRD and Product Increment files.

**CRITICAL - Verify Previous Work Committed**:

Before proceeding, verify your previous task was committed:
1. Run `git log --oneline -5` to check recent commits
2. Look for commits containing your agent type (e.g., "backend-engineer", "frontend-engineer")
3. If your previous task is uncommitted: **REFUSE to start** and respond with error explaining uncommitted work exists
4. Note: Changes from other engineers (parallel work) are expected and fine - only verify YOUR previous work is committed

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

Use the Task tool to call the appropriate reviewer subagent:
- Backend work → Use `backend-reviewer` subagent
- Frontend work → Use `frontend-reviewer` subagent
- E2E tests → Use `test-automation-reviewer` subagent

**Delegation format**:
```
Review the work I just completed

Request: [path from current-task.json: request_file_path]
Response: [path to response file you'll create: replace "request" with "response" and use task summary]
```

**Review loop**:
- If reviewer returns NOT APPROVED → Fix issues → Call reviewer subagent again
- If reviewer returns APPROVED → Reviewer commits automatically, proceed to next step

**STEP 9**: Re-read Product Increment, update plan if needed

**STEP 10**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

After completing all work AND receiving reviewer approval, you MUST call the MCP **CompleteAndExitTask** tool to signal completion. This tool call will IMMEDIATELY TERMINATE your session - there is no going back after this call.

**Before calling CompleteAndExitTask**:
1. Ensure all work is complete and all todos are marked as completed
2. Write a comprehensive response (what you accomplished, notes for Tech Lead)
3. Create an objective technical summary in sentence case (like a commit message)

**Call MCP CompleteAndExitTask tool**:
- `agentType`: Your agent type (backend-engineer, frontend-engineer, or test-automation-engineer)
- `taskSummary`: Objective technical description of what was implemented (imperative mood, sentence case). Examples: "Add team member endpoints with authorization", "Implement user avatar upload", "Fix null reference in payment processor". NEVER use subjective evaluations like "Excellent implementation" or "Clean code".
- `responseContent`: Your full response in markdown

⚠️ Your session terminates IMMEDIATELY after calling CompleteAndExitTask

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy the JSON from STEP 0**

**❌ DON'T: Create custom todo format**
