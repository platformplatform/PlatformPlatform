---
description: Implement a specific task from a Product Increment following the systematic workflow
argument-hint: [prd-path] [product-increment-path] [task-title] [context-message]
---

# Implement Task Workflow

PRD file: $1
Product Increment file: $2
Task to implement: $3
Context update: $4

## Context Efficiency

**If this is your first task**: Read the PRD file to understand the overall feature context. Read all Product Increment files and all rules.

**If you have context update ($4)**: The tech lead has provided file references to read for catching up efficiently. Read the specified files instead of re-reading everything.

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

**Step 1. Understand context and catch up efficiently**:
   - Mark "Understand context and catch up efficiently" [in_progress] in todo
   - **If context update provided ($4)**: Follow the specific instructions in $4 to catch up efficiently
   - **If no context update**: Read PRD file ($1), all Product Increment files, and check messages directory
   - Mark "Understand context and catch up efficiently" [completed] in todo

**Step 2. Study rules**:
   - Mark "Study rules" [in_progress] in todo
   - **If context update says "rules already studied"**: Skip this step
   - **If first time or context update says "read rules"**: Read ALL files in appropriate rules directory
   - **Backend**: /.claude/rules/backend/, **Frontend**: /.claude/rules/frontend/, **E2E**: /.claude/rules/end-to-end-tests/
   - Mark "Study rules" [completed] in todo

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
   - **CRITICAL**: The check command MUST exit with code 0 to be able to commit code
   - NEVER continue until ALL issues are fixed and check runs without errors
   - Gate rule: You cannot proceed until the check command completes successfully with exit code 0
   - Mark "Validate implementation builds and fix all static code analysis warnings" [completed] in todo

**Step 6. Mark task as Ready for Review**:
   - Mark "Mark task as Ready for Review" [in_progress] in todo
   - Edit the Product Increment file: change [In Progress] to [Ready for Review]
   - **CRITICAL**: NEVER mark task as [Completed] - only reviewers can mark tasks [Completed]
   - Mark "Mark task as Ready for Review" [completed] in todo

**Step 7. Critically evaluate remaining tasks and update plan**:
   - Mark "Critically evaluate remaining tasks and update plan" [in_progress] in todo
   - Re-read the ENTIRE Product Increment plan, focusing on remaining [Planned] tasks
   - Apply critical thinking: Based on what you learned implementing this task, evaluate each remaining task:
     * Does the task description clearly define what needs to be implemented?
     * Is the task still necessary given what was just implemented?
     * Is "Task # + 1" still the natural next step, or should task order change?
     * Should any tasks be split into smaller tasks?
     * Should any tasks be consolidated?
     * Are there new tasks needed that weren't originally planned?
   - **If changes needed**: Update the Product Increment file with your improvements
   - **If plan looks good**: No changes needed, proceed to next step
   - **Document changes**: If you updated the plan, note this in your response file (Step 8)
   - Mark "Critically evaluate remaining tasks and update plan" [completed] in todo

**Step 8. Create response file**:
   - Mark "Create response file" [in_progress] in todo
   - Create response file using atomic rename: .tmp → .md to signal completion
   - Include summary of implementation and any plan changes made in Step 7
   - **IMPORTANT**: Use descriptive response file names with proper casing for better activity display:
     * Format: `NNNN.{agent-type}.response.Title-Case-Task-Name.md`
     * Example: `0001.backend-engineer.response.Create-Team-Aggregate-With-Database-Migration.md`
     * Use Title-Case-With-Dashes so the activity display shows "Create Team Aggregate With Database Migration"
   - Mark "Create response file" [completed] in todo

## Todo list template

Use this exact format with nested structure:

```
Understand context and catch up efficiently [pending]                              (STEP 1)
Study rules relevant rules for the task at hand [pending]                          (STEP 2)
Research existing patterns for this task type [pending]                            (STEP 3)
Implement task [name of the task you have been asked to implement] [pending]       (STEP 4) *
├─  Task #.1 [Copy exact text from Product Increment file] [pending]
├─  Task #.2 [Copy exact text from Product Increment file] [pending]
└─  Task #.N [Copy exact text from Product Increment file] [pending]
Validate implementation builds and fix all static code analysis warnings [pending] (STEP 5)
Mark task as Ready for Review [pending]                                            (STEP 6)
Critically evaluate remaining tasks and update plan [pending]                      (STEP 7)
Create response file [pending]                                                     (STEP 8)
```

**CRITICAL: Use these exact prefixes for proper visual hierarchy:**
- `├─` for subtask items (not the last in their group)
- `└─` for the last subtask item

**DO NOT CHANGE THE WORDING**:
- DO NOT write "Analyze codebase structure" or similar
- COPY THE EXACT TEXT: "Study ALL rules for this task type"
- COPY THE EXACT TEXT: "Research existing patterns for this task type"
- COPY THE EXACT TEXT: "Mark task as Ready for Review" (STEP 6)
- COPY THE EXACT TEXT: "Critically evaluate remaining tasks and update plan" (STEP 7)

Continuously use TodoWrite tool to update todo list as you work through them.