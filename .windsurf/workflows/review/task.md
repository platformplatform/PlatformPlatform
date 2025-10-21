---
description: Workflow for task title to review (e.g., "add user filtering")
auto_execution_mode: 1
---

# Review Task Workflow

You are reviewing: **{{{title}}}**

## Review Principles

**Devil's Advocate Mindset**: Your job is to validate the engineer's work by actively searching for problems. Look for inconsistencies, deviations, and potential issues.

**Zero Tolerance**: ALL findings must be fixed, regardless of severity. Never dismiss issues as "minor" or "not worth fixing". Every deviation from rules or established patterns must be addressed.

**Evidence-Based Reviews**: Every finding must be backed by:
1. Explicit rules from `.claude/rules/` files, OR
2. Established patterns found elsewhere in the codebase (cite specific file:line examples), OR
3. Well-established ecosystem conventions (e.g., .NET interfaces prefixed with `I`)

Avoid subjective personal preferences.

**Line-by-Line Review**: Like GitHub PR reviews - comment ONLY on specific file:line combinations that have issues. NO comments on correct code. NO commentary on what was done well.

**Objective Language**: State facts about rule violations or pattern deviations. Reference specific rules or codebase examples. Avoid subjective evaluations or praise.

**Concise Communication**: Minimize token usage for the engineer. Focus only on what needs fixing.

---

## STEP 0: Read Task Assignment

**Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
- `requestFilePath`: Full path to your request file
- `prdPath`: Path to PRD (if Product Increment task)
- `productIncrementPath`: Path to Product Increment (if applicable)
- `taskNumberInIncrement`: Task number in the increment (if applicable)
- `title`: Task title

**Then read the request file** from the path in `requestFilePath`.

**If `prdPath` exists in current-task.json:**
1. Read PRD from the path in `prdPath`
2. Read Product Increment plan from the path in `productIncrementPath`
3. Understand the task (`taskNumberInIncrement`) within the larger feature context

**Read all files referenced in the engineer's request** (implementation details, changed files, etc.).

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
    {"content": "Run validation tools in parallel (format, test, inspect)", "status": "pending", "activeForm": "Running validation tools in parallel"},
    {"content": "Study ALL rules in .claude/rules/{backend|frontend|end-to-end-tests}/", "status": "pending", "activeForm": "Studying all rules for my role"},
    {"content": "Verify translations are complete and use consistent domain terminology (frontend-reviewer only)", "status": "pending", "activeForm": "Verifying translations"},
    {"content": "Review each changed file in detail", "status": "pending", "activeForm": "Reviewing each changed file"},
    {"content": "Review high level architecture (make a very high level review)", "status": "pending", "activeForm": "Reviewing high level architecture"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making binary decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing changes if approved"},
    {"content": "Update Product Increment status with [Completed] or [Changes Required]", "status": "pending", "activeForm": "Updating Product Increment status"},
    {"content": "MANDATORY: Call CompleteWork (approved or rejected) to signal completion", "status": "pending", "activeForm": "Calling CompleteWork to signal completion"}
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

**STEP 3**: Study ALL rules for your role (read files or recall from memory)

- **Backend reviewer**: ALL files in `.claude/rules/backend/`
- **Frontend reviewer**: ALL files in `.claude/rules/frontend/`
- **Test automation reviewer**: ALL files in `.claude/rules/end-to-end-tests/`

**STEP 4**: Frontend only - verify translations in `*.po` files

Check all `*.po` files for empty `msgstr ""` entries and inconsistent domain terminology. Reject if translations are missing or terminology differs from established usage elsewhere.

**STEP 5**: Review each file line-by-line

**STEP 6**: Review architecture

**STEP 7**: Decide - APPROVED or NOT APPROVED

**STEP 8**: If APPROVED, commit changes and get commit hash

1. Extract "Files Changed" from engineer's request
2. Verify scope completeness:
   - Run `git status --porcelain` to see all changed files
   - Filter to YOUR scope only:
     - **Backend reviewer**: Api/Core/Tests files + *.Api.json files (even in WebApp folder)
     - **Frontend reviewer**: WebApp files EXCEPT *.Api.json files
   - Verify engineer's list matches filtered git status EXACTLY (no missing files, no extra files)
   - If mismatch: REJECT with specific files missing or wrongly included

**Execute steps 3-6 immediately without delay (minimize race conditions)**:

3. Stage files: `git add <file>` for each file in engineer's list
4. Verify: `git diff --cached --name-only` matches engineer's list exactly. If not: REJECT
5. Commit with descriptive message
6. Get hash: `git rev-parse HEAD`

🚨 **NEVER use `git add -A` or `git add .`**
🚨 **Execute git commands immediately** - no other work between staging and committing

**Edge case**: If `git status` shows no changes (verification-only), use `git rev-parse HEAD` for commitHash.

**STEP 9**: Edit Product Increment status

Update the Product Increment file:
- If APPROVED: Change status to `[Completed]`
- If REJECTED: Change status to `[Changes Required]`

**STEP 10**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

Call MCP **CompleteWork** tool with `mode: "review"` - your session terminates IMMEDIATELY after this call.

**For APPROVED reviews**:
- Provide: `mode: "review"`
- Provide: `commitHash` (from `git rev-parse HEAD` in STEP 8)
- Provide: `rejectReason` as null or empty string

**For REJECTED reviews**:
- Provide: `mode: "review"`
- Provide: `commitHash` as null or empty string
- Provide: `rejectReason` (sentence case, imperative mood)

---

## Response Format Requirements

When calling CompleteWork with `responseContent`:

**For REJECTED reviews**:

```markdown
[Short objective summary of why rejected - 1-2 sentences or short paragraph if more elaboration needed]

## Issues

### File.cs:Line
[Objective description of problem]
- **Rule/Pattern**: [Reference to .claude/rules/X.md or pattern from codebase]
- **Fix**: [Optional: Suggest specific change]

### AnotherFile.cs:Line
[Objective description of problem]
- **Rule/Pattern**: [Reference]
- **Fix**: [Optional]
```

**For APPROVED reviews**:

```markdown
[One sentence objective explanation of why approved, e.g., "Follows established patterns for X and complies with rules Y and Z"]
```

**Critical Requirements**:
- Line-by-line review like GitHub PR
- NO comments on correct code
- NO subjective language ("excellent", "great", "well done")
- NO dismissing issues as "minor" or "optional"
- Cite specific rules or codebase patterns
- Keep responses concise to minimize token usage

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy JSON from STEP 0**

**❌ DON'T: Create custom format**