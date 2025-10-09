---
description: Review a specific task implementation from a Product Increment following the systematic review workflow
---

# Review Task Workflow

## STEP 0: Create Todo List - DO THIS NOW!

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Understand context and catch up efficiently", "status": "pending", "activeForm": "Understanding context and catching up"},
    {"content": "Validate implementation builds by running check command", "status": "pending", "activeForm": "Validating implementation builds"},
    {"content": "Study rules relevant for the task at hand", "status": "pending", "activeForm": "Studying relevant rules"},
    {"content": "Review each changed file in detail", "status": "pending", "activeForm": "Reviewing each changed file"},
    {"content": "Review high level architecture (make a very high level review)", "status": "pending", "activeForm": "Reviewing high level architecture"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making binary decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing changes if approved"},
    {"content": "Update Product Increment status with [Completed] or [Changes Required]", "status": "pending", "activeForm": "Updating Product Increment status"},
    {"content": "Call /complete/review to signal completion", "status": "pending", "activeForm": "Calling completion command"}
  ]
}
```

After creating base todo, expand "Review each changed file" with files from `git status --porcelain`.

---

## Context Discovery

Find your request file in `../messages/*.{agent-type}.request.*.md` and read it.

**Request contains**:
- Task title, PRD path, Product Increment path
- Engineer's request and response files

Read all referenced files.

---

## Workflow Steps

**STEP 1**: Read all context files

**STEP 2**: Run **check MCP tool**, must pass

**STEP 3**: Study rules

**STEP 4**: Review each file line-by-line

**STEP 5**: Review architecture

**STEP 6**: Decide - APPROVED or NOT APPROVED

**STEP 7**: If APPROVED, run `/review/commit`

**STEP 8**: Edit Product Increment status

**STEP 9**: Call `/complete/review`

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy JSON from STEP 0**

**❌ DON'T: Create custom format**
