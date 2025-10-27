---
description: Workflow for task title to review (e.g., "add user filtering")
auto_execution_mode: 1
---

# Review Task Workflow

You are reviewing: **{{{title}}}**

## Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

## STEP 1: Read Task Assignment

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

**Read your `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{your-agent-type}/current-task.json`** to get:
- `requestFilePath`: Request file path
- `storyId`: [StoryId]
- `taskId`: [TaskId]
- `taskTitle`: Task title

**Then read the request file** from the path in `requestFilePath`.

**If `storyId` exists in current-task.json AND `storyId` is not "ad-hoc":**
1. Read [story] from `storyId`
2. Understand the [task] (`taskId`) within the [story] context

**If `storyId` is "ad-hoc":**
- Skip [PRODUCT_MANAGEMENT_TOOL] operations
- Review and commit exactly like regular [tasks]

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
    {"content": "Test changes in Chrome DevTools and verify zero network warnings and console errors (frontend-reviewer only)", "status": "pending", "activeForm": "Testing in Chrome DevTools and verifying no issues"},
    {"content": "Review each changed file in detail", "status": "pending", "activeForm": "Reviewing each changed file"},
    {"content": "Review high level architecture (make a very high level review)", "status": "pending", "activeForm": "Reviewing high level architecture"},
    {"content": "Make binary decision (approve or reject)", "status": "pending", "activeForm": "Making binary decision"},
    {"content": "If approved, commit changes", "status": "pending", "activeForm": "Committing changes if approved"},
    {"content": "Update [story] status to [Completed] or [Active]", "status": "pending", "activeForm": "Updating [story] status"},
    {"content": "MANDATORY: Call CompleteWork (approved or rejected) to signal completion", "status": "pending", "activeForm": "Calling CompleteWork to signal completion"}
  ]
}
```

After creating base todo, expand "Review each changed file" with files from `git status --porcelain`.

---

## Workflow Steps

**STEP 1**: Read all context files

**STEP 2**: Run validation tools in parallel (backend [tasks] only)

For **backend [tasks]**, run **format**, **test**, and **inspect** in parallel using the Task tool:
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

For **frontend [tasks]**, use **test** and **inspect** MCP tools directly.

**STEP 3**: Study ALL rules for your role (read files or recall from memory)

- **Backend reviewer**: ALL files in `.claude/rules/backend/`
- **Frontend reviewer**: ALL files in `.claude/rules/frontend/`
- **Test automation reviewer**: ALL files in `.claude/rules/end-to-end-tests/`

**STEP 4**: Frontend only - verify translations in `*.po` files

Check all `*.po` files for empty `msgstr ""` entries and inconsistent domain terminology. Reject if translations are missing or terminology differs from established usage elsewhere.

**STEP 5**: Frontend only - test changes in Chrome DevTools with ZERO TOLERANCE

**MANDATORY FOR FRONTEND REVIEWER - DO NOT SKIP**

1. **Navigate to https://localhost:9000** and test the changes:
   - Test all functionality that was implemented
   - Verify UI components render correctly
   - Test user interactions (clicks, forms, navigation, etc.)
   - If the website is not responding, use the **watch** MCP tool to restart the server (restarts .NET Aspire and runs database migrations in background)

2. **Monitor Network tab** - Document ALL issues:
   - **Zero tolerance**: No failed requests, no 4xx/5xx errors
   - No slow requests without explanation
   - Document ANY network warnings or errors (even if pre-existing per Boy Scout rule)

3. **Monitor Console tab** - Document ALL issues:
   - **Zero tolerance**: No console errors, no warnings
   - Document ANY console errors or warnings (even if pre-existing per Boy Scout rule)
   - Clear console and verify it stays clean during all interactions

4. **Login instructions**:
   - Username: `admin@platformplatform.local`
   - Use `UNLOCK` for verification code (works on localhost only)
   - If user doesn't exist: Sign up for a new tenant, use `UNLOCK` for verification code

**Boy Scout Rule**: Leave the codebase cleaner than you found it. If you see pre-existing console errors or network warnings unrelated to your changes, DOCUMENT THEM. Zero tolerance means ZERO - not "only for my changes".

**STEP 6**: Review each file line-by-line

**STEP 7**: Review architecture

**STEP 8**: Decide - APPROVED or NOT APPROVED

**STEP 9**: If APPROVED, commit changes and get commit hash

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

**STEP 10**: Update [task] status in `[PRODUCT_MANAGEMENT_TOOL]`

**If `storyId` is not "ad-hoc":**
- If APPROVED: Update [task] status to [Completed]
- If REJECTED: Update [task] status back to [Active]

**If `storyId` is "ad-hoc":**
- Skip [PRODUCT_MANAGEMENT_TOOL] status updates

**STEP 11**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

Call MCP **CompleteWork** tool with `mode: "review"` - your session terminates IMMEDIATELY after this call.

**Categorized Feedback Required**:
Use category prefixes for all feedback:
- `[system]` - Workflow, MCP tools, agent coordination, message handling
- `[requirements]` - Requirements clarity, acceptance criteria, task description
- `[code]` - Code patterns, rules, architecture guidance

Examples:
- `[system] Validation tools reported stale results from previous run`
- `[requirements] Engineer's file list didn't match git status - unclear which files were in scope`
- `[code] Missing examples for implementing telemetry in this pattern`

**For APPROVED reviews**:
- Provide: `mode: "review"`
- Provide: `commitHash` (from `git rev-parse HEAD` in STEP 9)
- Provide: `rejectReason` as null or empty string
- Provide: `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes

**For REJECTED reviews**:
- Provide: `mode: "review"`
- Provide: `commitHash` as null or empty string
- Provide: `rejectReason` (sentence case, imperative mood)
- Provide: `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes

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