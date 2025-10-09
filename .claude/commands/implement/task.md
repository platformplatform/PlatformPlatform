---
description: Implement a specific task from a Product Increment following the systematic workflow
---

# Implement Task Workflow

**IMPORTANT**: If you're reading this, the slash command worked! If you DON'T see this text and only see `/implement/task`, then:
1. Read `.claude/commands/implement/task.md` (this file)
2. Find your request: `../messages/*.{your-agent-type}.request.*.md`
3. Follow the workflow below

---

## STEP 0: Create Todo List - DO THIS NOW!

**YOU MUST CREATE THE TODO LIST EXACTLY LIKE THIS NOW**:

```
Understand context and catch up efficiently [pending]                              (STEP 1)
Study relevant rules for the task at hand [pending]                                (STEP 2)
Research existing patterns for this task type [pending]                            (STEP 3)
Implement task [name of the task you have been asked to implement] [pending]       (STEP 4) *
├─  Task #.1 [Copy exact text from Product Increment file] [pending]
├─  Task #.2 [Copy exact text from Product Increment file] [pending]
└─  Task #.N [Copy exact text from Product Increment file] [pending]
Validate implementation builds and fix all static code analysis warnings [pending] (STEP 5)
Validate translations (frontend tasks only) [pending]                              (STEP 6)
Mark task as Ready for Review [pending]                                            (STEP 7)
Critically evaluate remaining tasks and update plan [pending]                      (STEP 8)
Call /complete/task to signal completion [pending]                                 (STEP 9)
```

### Examples

**✅ DO: Copy exact wording**
```
Understand context and catch up efficiently [pending]                              (STEP 1)
Study relevant rules for the task at hand [pending]                                (STEP 2)
Research existing patterns for this task type [pending]                            (STEP 3)
```

**❌ DON'T: Rewrite or simplify**
```
Analyze codebase structure [pending]
Read project documentation [pending]
Find similar code [pending]
```

**✅ DO: Use exact STEP labels**
```
Mark task as Ready for Review [pending]                                            (STEP 7)
Critically evaluate remaining tasks and update plan [pending]                      (STEP 8)
```

**❌ DON'T: Change step names**
```
Update task status [pending]
Review remaining work [pending]
```

**DO NOT CHANGE THE WORDING** - Copy the exact text shown in the template above

---

## Context Discovery

You are a worker agent. Find your request:

1. **Check your working directory**: You're in `.workspace/agent-workspaces/{branch}/{agent-type}/`
2. **Find request file**: Look in `../messages/` for most recent `*.{agent-type}.request.*.md`
3. **Read request file**: It contains your task instructions

**Request file format**:
```
Please implement task "Task title" from {product-increment-path}

Product Requirements Document: {prd-path}
```

Extract:
- **Task title**: The quoted text after "task"
- **Product Increment path**: After "from"
- **PRD path**: After "Product Requirements Document:"

---

## Workflow Steps

**STEP 1**: Understand context
- Read PRD file
- Read Product Increment file
- Find your task in Product Increment (match title from request)
- Extract ALL subtasks for your task
- Update todo: Add subtasks under Step 4, mark Step 1 [completed]

**STEP 2**: Study rules
- Backend: Read `.claude/rules/backend/`
- Frontend: Read `.claude/rules/frontend/`
- Mark [completed]

**STEP 3**: Research patterns
- Search codebase for similar implementations
- Mark [completed]

**STEP 4**: Implement
- For each subtask: Mark [in_progress], implement, mark [completed]
- Use **build** and **test** MCP tools frequently
- Mark main task [completed]

**STEP 5**: Validate
- Backend: Run **check MCP tool** for backend (must pass with zero findings)
- Frontend: Run **check MCP tool** for frontend (must pass)
- Fix ALL warnings/errors
- Mark [completed]

**STEP 6**: Validate translations (frontend only)
- Check `git diff --name-only | grep "\.po$"`
- Review translation entries
- Mark [completed]

**STEP 7**: Mark task Ready for Review
- Edit Product Increment file: Change `[In Progress]` to `[Ready for Review]`
- NEVER mark `[Completed]` - only reviewers do that
- Mark [completed]

**STEP 8**: Evaluate remaining tasks
- Re-read Product Increment file
- Critically assess if plan still makes sense
- Update plan if needed
- Mark [completed]

**STEP 9**: Signal completion
- Call `/complete/task` slash command
- Provide summary and full response
- Your session will terminate

---

## REMINDER: Todo List Format

**THE TODO LIST MUST FOLLOW THIS EXACT FORMAT**:

```
Understand context and catch up efficiently [pending]                              (STEP 1)
Study relevant rules for the task at hand [pending]                                (STEP 2)
Research existing patterns for this task type [pending]                            (STEP 3)
Implement task [name of the task you have been asked to implement] [pending]       (STEP 4) *
├─  Task #.1 [Copy exact text from Product Increment file] [pending]
├─  Task #.2 [Copy exact text from Product Increment file] [pending]
└─  Task #.N [Copy exact text from Product Increment file] [pending]
Validate implementation builds and fix all static code analysis warnings [pending] (STEP 5)
Validate translations (frontend tasks only) [pending]                              (STEP 6)
Mark task as Ready for Review [pending]                                            (STEP 7)
Critically evaluate remaining tasks and update plan [pending]                      (STEP 8)
Call /complete/task to signal completion [pending]                                 (STEP 9)
```

**DO NOT CHANGE THE WORDING**:
- ❌ DON'T write "Analyze codebase structure"
- ✅ DO copy "Study relevant rules for the task at hand"
- ❌ DON'T write "Find similar implementations"
- ✅ DO copy "Research existing patterns for this task type"
- ❌ DON'T write "Update task status"
- ✅ DO copy "Mark task as Ready for Review"

---

## Critical Rules

- Use **build**, **test**, **check**, **e2e** MCP tools ONLY
- Never use `dotnet`, `npm`, `npx` commands directly
- Follow todo list exactly
- Update todo continuously
- Never skip steps
