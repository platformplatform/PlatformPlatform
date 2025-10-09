---
description: Implement a specific task from a Product Increment following the systematic workflow
---

# Implement Task Workflow

## STEP 0: Create Todo List - DO THIS NOW!

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
    {"content": "Critically evaluate remaining tasks and update plan", "status": "pending", "activeForm": "Evaluating remaining tasks"},
    {"content": "Call /complete/task to signal completion", "status": "pending", "activeForm": "Calling completion command"}
  ]
}
```

After creating base todo, expand "Implement task" with subtasks from Product Increment file.

---

## Context Discovery

Find your request file in `../messages/*.{agent-type}.request.*.md` and read it.

Example: `../messages/0001.backend-engineer.request.create-team-aggregate.md`

**Request contains**:
- Task title (what to implement)
- Product Increment file path
- PRD file path

Read all three files to understand context.

---

## Workflow Steps

**STEP 1**: Read PRD, Product Increment, find your task, extract subtasks, update todo

**STEP 2**: Study rules (`.claude/rules/backend/` or `.claude/rules/frontend/`)

**STEP 3**: Research similar implementations in codebase

**STEP 4**: Implement each subtask, use **build** and **test** MCP tools

**STEP 5**: Run **check MCP tool**, fix all warnings (must pass)

**STEP 6**: Frontend only - validate translations

**STEP 7**: Edit Product Increment file: `[In Progress]` → `[Ready for Review]`

**STEP 8**: Re-read Product Increment, update plan if needed

**STEP 9**: Call `/complete/task`, provide summary and response

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy the JSON from STEP 0**

**❌ DON'T: Create custom todo format**
