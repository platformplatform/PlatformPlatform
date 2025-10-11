---
description: Workflow for task title to review (e.g., "add user filtering")
auto_execution_mode: 1
---

# Review Task Workflow

You are reviewing: **{{{title}}}**

## STEP 0: Read Task Assignment

**Read `current-task.json` in your workspace root** to get:
- `request_file_path`: Full path to your request file
- `prd_path`: Path to PRD (if Product Increment task)
- `product_increment_path`: Path to Product Increment (if applicable)
- `task_number_in_increment`: Task number in the increment (if applicable)
- `title`: Task title

**Then read the request file** from the path in `request_file_path`.

**Request file contains**:
- Engineer type being reviewed
- PRD path (if applicable)
- Product Increment path (if applicable)
- Task title
- Engineer's request file path
- Engineer's response file path

Read all referenced files to understand what was implemented.

---

## STEP 1: Create Todo List - DO THIS NOW!

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Understand context and catch up efficiently", "status": "pending", "activeForm": "Understanding context and catching up"},
    {"content": "Run validation tools in parallel (format, test, inspect)", "status": "pending", "activeForm": "Running validation tools in parallel"},
    {"content": "Study rules relevant for the task at hand", "status": "pending", "activeForm": "Studying relevant rules"},
    {"content": "Review each changed file in detail", "status": "pending", "activeForm": "Reviewing each changed file"},
    {"content": "Review high level architecture (make a very high level review)", "status": "pending", "activeForm": "Reviewing high level architecture"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making binary decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing changes if approved"},
    {"content": "Update Product Increment status with [Completed] or [Changes Required]", "status": "pending", "activeForm": "Updating Product Increment status"}
  ]
}
```

After creating base todo, expand "Review each changed file" with files from `git status --porcelain`.

---

## Workflow Steps

**STEP 1**: Read all context files

**STEP 2**: Run validation tools in parallel (backend tasks only)

For **backend tasks**, run **format**, **test**, and **inspect** in parallel using the Task tool:
- Spawn three `backend-tool-runner` subagents simultaneously
- One runs `format`, one runs `test`, one runs `inspect`
- Wait for all three to complete
- All must pass (reject if any fail)

**Parallel execution example**:
```
In a single message, use Task tool three times:
1. Task tool → backend-tool-runner: "Run backend tool: format"
2. Task tool → backend-tool-runner: "Run backend tool: test"
3. Task tool → backend-tool-runner: "Run backend tool: inspect"
```

For **frontend tasks**, use **test** and **inspect** MCP tools directly.

**STEP 3**: Study rules

**STEP 4**: Review each file line-by-line

**STEP 5**: Review architecture

**STEP 6**: Decide - APPROVED or NOT APPROVED

**STEP 7**: If APPROVED, run `/review/commit`

**STEP 8**: Edit Product Increment status

**STEP 9**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

After completing your review, you MUST call the MCP **CompleteAndExitReview** tool to signal completion. This tool call will IMMEDIATELY TERMINATE your session - there is no going back after this call.

**Before calling CompleteAndExitReview**:
1. Ensure all todos are marked as completed
2. Make your binary decision: APPROVED or NOT APPROVED
3. Write comprehensive review feedback
4. Create a brief summary in sentence case (e.g., "Excellent implementation" or "Missing test coverage")

**Call MCP CompleteAndExitReview tool**:
- `agentType`: Your agent type (backend-reviewer, frontend-reviewer, or test-automation-reviewer)
- `approved`: true or false
- `reviewSummary`: Your brief summary
- `responseContent`: Your full review feedback in markdown

⚠️ Your session terminates IMMEDIATELY after calling CompleteAndExitReview

**Examples of good summaries**:
- ✅ "Excellent implementation"
- ✅ "Missing test coverage"
- ✅ "Clean architecture and comprehensive tests"
- ✅ "Incorrect use of strongly typed IDs"
- ❌ "Good" (too vague)
- ❌ "LGTM" (unclear)

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy JSON from STEP 0**

**❌ DON'T: Create custom format**