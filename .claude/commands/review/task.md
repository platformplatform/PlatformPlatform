---
description: Review a specific task implementation from a Product Increment following the systematic review workflow
---

# Review Task Workflow

## STEP 0: Create Todo List (DO THIS FIRST!)

**CRITICAL**: Before doing ANYTHING else, create this exact todo list:

```
Understand context and catch up efficiently [pending]                                                       (STEP 1)
Validate implementation builds by running check command [pending]                                           (STEP 2)
Study rules relevant for the task at hand [pending]                                                         (STEP 3)
Review each changed file in detail [pending]                                                                (STEP 4) *
├─  Review [filename.ext] and ensure all lines, methods, properties, classes follow ALL rules [pending]
├─  Review [filename.ext] and ensure all lines, methods, properties, classes follow ALL rules [pending]
└─  Review [filename.ext] and ensure all lines, methods, properties, classes follow ALL rules [pending]
Review high level architecture (make a very high level review) [pending]                                   (STEP 5)
Make binary decision (approve or reject) [pending]                                                          (STEP 6)
If approved, commit changes [pending]                                                                       (STEP 7)
Update Product Increment status with [Completed] or [Changes Required] [pending]                            (STEP 8)
Call /complete/review to signal completion [pending]                                                        (STEP 9)
```

**DO NOT CHANGE THE WORDING** - Copy exactly as shown above

---

## Context Discovery

You are a reviewer agent. Find your request:

1. **Check your working directory**: You're in `.workspace/agent-workspaces/{branch}/{agent-type}/`
2. **Find request file**: Look in `../messages/` for most recent `*.{agent-type}.request.*.md`
3. **Read request file**: It contains review instructions

**Request file format**:
```
Please review task "Task title" from {product-increment-path}

Product Requirements Document: {prd-path}
Product Increment: {product-increment-path}
Task: "Task title"
Request: {engineer-request-file}
Response: {engineer-response-file}
```

Extract paths and read all referenced files.

---

## Workflow Steps

**STEP 1**: Understand context
- Read PRD, Product Increment, engineer's request and response
- Mark [completed]

**STEP 2**: Validate builds
- Run **check MCP tool** (backend or frontend)
- Must pass with zero findings
- Mark [completed]

**STEP 3**: Study rules
- Backend: `.claude/rules/backend/`
- Frontend: `.claude/rules/frontend/`
- Mark [completed]

**STEP 4**: Review each file
- For each file: Read line-by-line, check against ALL rules
- Document violations with rule citations
- Mark each file [completed]

**STEP 5**: Review architecture
- High-level patterns and design
- Mark [completed]

**STEP 6**: Make decision
- **APPROVED**: Zero required changes
- **NOT APPROVED**: Any findings that must be fixed
- Mark [completed]

**STEP 7**: Commit if approved
- If APPROVED: Run `/review/commit` with commit message
- If NOT APPROVED: Skip
- Mark [completed]

**STEP 8**: Update status
- If APPROVED: Change `[Ready for Review]` to `[Completed]`
- If NOT APPROVED: Change to `[Changes Required]`
- Mark [completed]

**STEP 9**: Signal completion
- Call `/complete/review` slash command
- Specify approved (true/false) and summary
- Your session will terminate

---

## Critical Rules

- Quality is highest priority
- Cannot approve if you have recommendations
- Use **check** MCP tool, never `dotnet` commands
- Follow todo list exactly
- Update todo continuously
