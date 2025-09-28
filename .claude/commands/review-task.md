---
description: Review a specific task implementation from a Product Increment following the systematic review workflow
argument-hint: [prd-path] [product-increment-path] [task-title] [request-file] [response-file] [context-message]
---

# Review Task Workflow

PRD file: $1
Product Increment file: $2
Task being reviewed: $3
Engineer's request file: $4
Engineer's response file: $5
Context update: $6

## Review Efficiency

**If this is your first review**: Read PRD file, Product Increment file, and all applicable rules.

**If you have context update ($6)**: The coordinator has provided file references to read for catching up efficiently. Read the specified files instead of re-reading everything.

## Multiple Request Handling

**If you see multiple request files when starting**:
1. **Read ALL request files** in chronological order (0001, 0002, 0003...)
2. **Understand the sequence** - Later requests might modify or clarify earlier ones
3. **Review based on the FINAL/LATEST request** - This supersedes earlier requests
4. **Create ONE response file** for the latest request only
5. **Don't respond to superseded requests** - Only the final request matters

## Mandatory Review Workflow

**This workflow is MANDATORY** - Follow every step exactly.

**Step 1. Understand context efficiently**:
   - **If context update provided ($6)**: Follow the specific instructions in $6 to catch up efficiently
   - **If no context update**: Read PRD file ($1), Product Increment file ($2), request file ($4), response file ($5)
   - Always check `/application/result.json` for any static code analysis findings

**Step 2. Apply rules**:
   - **If context update says "rules already applied"**: Skip detailed rule reading
   - **If first time or context update says "apply rules"**: Read and apply ALL files in appropriate rules directory
   - **Backend**: /.claude/rules/backend/, **Frontend**: /.claude/rules/frontend/, **E2E**: /.claude/rules/end-to-end-tests/
   - Review all changed files against applicable rules

**Step 3. Make binary decision**:
   - **APPROVED**: Zero findings or only minor suggestions that don't affect functionality
   - **NOT APPROVED**: Any findings that must be fixed

**Step 4. Create response file**:
   - Create response file with clear "## DECISION: APPROVED" or "## DECISION: NOT APPROVED - REQUIRES FIXES"
   - If APPROVED: Use SlashCommand tool to run `/commit-changes` with descriptive commit message
   - If NOT APPROVED: List all findings that must be addressed
   - Use atomic rename: .tmp → .md to signal completion

## Quality Standards

If you have recommendations or suggestions, you CANNOT approve. Quality is the highest priority. Only approve when implementation is production-ready with zero required changes.