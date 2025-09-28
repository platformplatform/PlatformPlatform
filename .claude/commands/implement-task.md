---
description: Implement a specific task from a Product Increment following the systematic workflow
argument-hint: [prd-path] [product-increment-path] [task-title]
---

# Implement Task Workflow

PRD file: $1
Product Increment file: $2
Task to implement: $3

Read the PRD file to understand the overall feature context and business requirements. Read the Product Increment file to understand your specific task and extract all subtasks.

## Multiple Request Handling

**If you see multiple request files when starting**:
1. **Read ALL request files** in chronological order (0001, 0002, 0003...)
2. **Understand the sequence** - Later requests might modify or clarify earlier ones
3. **Implement the FINAL/LATEST request** - This supersedes earlier requests
4. **Create ONE response file** for the latest request only
5. **Don't respond to superseded requests** - Only the final request matters

## Mandatory workflow

**This workflow is MANDATORY** - Follow every step exactly.

**Step 0. Create todo list**:
   - Create a todo list with all workflow steps using the template below
   - Read the Product Increment file ($2) and find your assigned task ($3)
   - Extract ALL subtasks from your assigned task and add as nested items under STEP 4
   - Set all tasks to [pending]

**Step 1. Understand full context and catch up on previous work**:
   - Mark "Understand full context and catch up on previous work" [in_progress] in todo
   - Read the PRD file ($1) to understand the overall feature context
   - Read ALL Product Increment files in the directory to understand the complete plan
   - List all files in `/.claude/agent-workspaces/[current-branch]/messages/` to see what work has been done
   - Read recent request and response files to understand what other agents have accomplished
   - Read any updated Product Increment plans to see what has changed since you were last active
   - Mark "Understand full context and catch up on previous work" [completed] in todo

**Step 2. Study all rules for this task type**:
   - Mark "Study ALL rules for this task type" [in_progress] in todo
   - **Backend**: Read ALL files in /.claude/rules/backend/
   - **Frontend**: Read ALL files in /.claude/rules/frontend/
   - **E2E**: Read ALL files in /.claude/rules/end-to-end-tests/
   - Mark "Study ALL rules for this task type" [completed] in todo

**Step 3. Research existing patterns for this task type**:
   - Mark "Research existing patterns for this task type" [in_progress] in todo
   - Study similar implementations in codebase for all the subtasks that will be implemented in step 4
   - Validate approach matches established patterns
   - Mark "Research existing patterns for this task type" [completed] in todo

**Step 4. Implement all subtasks**:
   - Mark main task [in_progress] in todo
   - For each subtask:
      - Mark subtask [in_progress] in todo
      - Implement subtask following established patterns
      - For changes run `pp build --backend`, `pp build --frontend`, and/or `pp test` continuously
      - Mark subtask [completed] in todo
   - Mark main task [completed] in todo

**Step 5. Validate implementation builds and fix all static code analysis warnings**:
   - Mark "Validate implementation builds and fix all static code analysis warnings" [in_progress] in todo
   - **Backend tasks**: Run `pp check` - all must pass with zero findings
   - **Frontend tasks**: Run `pp check --frontend` - all must pass
   - **E2E tasks**: Run `pp e2e` - all must pass
   - If you see "Backend issues found. Opening result.json...", fix all static code analysis findings before proceeding
   - Gate rule: You cannot proceed until output shows "Build succeeded" and "No backend issues found!"
   - Mark "Validate implementation builds and fix all static code analysis warnings" [completed] in todo

**Step 6. Evaluate and update Product Increment plan**:
   - Mark "Evaluate and update Product Increment plan" [in_progress] in todo
   - Re-read the Product Increment plan and evaluate if remaining tasks need updates
   - Update the plan if implementation insights suggest changes
   - Mark "Evaluate and update Product Increment plan" [completed] in todo

**Step 7. Create response file**:
   - Mark "Create response file" [in_progress] in todo
   - Create response file using atomic rename: .tmp → .md to signal completion
   - Mark "Create response file" [completed] in todo

## Todo list template

Use this exact format with nested structure:

```
Understand full context and catch up on previous work [pending]             (STEP 1)
Study ALL rules for this task type [pending]                                (STEP 2)
Research existing patterns for this task type [pending]                     (STEP 3)
Implement task [name of the task you have been asked to implement] [pending] (STEP 4) *
├─  Task #.1 [Copy exact text from Product Increment file] [pending]
├─  Task #.2 [Copy exact text from Product Increment file] [pending]
└─  Task #.N [Copy exact text from Product Increment file] [pending]
Validate implementation builds and fix all static code analysis warnings [pending] (STEP 5)
Evaluate and update Product Increment plan [pending]                        (STEP 6)
Create response file [pending]                                              (STEP 7)
```

**CRITICAL: Use these exact prefixes for proper visual hierarchy:**
- `├─` for subtask items (not the last in their group)
- `└─` for the last subtask item

**DO NOT CHANGE THE WORDING**:
- DO NOT write "Analyze codebase structure" or similar
- COPY THE EXACT TEXT: "Study ALL rules for this task type"
- COPY THE EXACT TEXT: "Research existing patterns for this task type"

Continuously use TodoWrite tool to update todo list as you work through them.