---
description: Implement a specific [task] from a [story] following the systematic workflow
args:
  - name: title
    description: Task title (passed from CLI, matches taskTitle in current-task.json)
    required: false
---

# Implement Task Workflow

You are implementing: **{{{title}}}**

**Note:** The [taskId] and [storyId] come from current-task.json, not from command arguments. The CLI passes only the [taskTitle] as the slash command argument.

## Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.claude/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

## STEP 1: Read Task Assignment

**Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
- `requestFilePath`: Request file path
- `storyId`: [StoryId]
- `taskId`: [TaskId]
- `taskTitle`: Task title

**Then read the request file** from the path in `requestFilePath`.

**If `storyId` exists in current-task.json AND `storyId` is not "ad-hoc":**
1. Read [story] from `storyId`
2. Understand your [task] (`taskId`) within the [story] context
3. **Update [task] status to [Active]** in `[PRODUCT_MANAGEMENT_TOOL]`

**If `storyId` is "ad-hoc":**
- Skip [PRODUCT_MANAGEMENT_TOOL] operations
- Still follow full engineer → reviewer → commit cycle

**CRITICAL - Verify Previous Work Committed**:

Before proceeding, verify your previous task was committed:
1. Run `git log --oneline -5` to check recent commits
2. Look for commits containing your agent type (e.g., "backend-engineer", "frontend-engineer")
3. If your previous task is uncommitted: **REFUSE to start** and respond with error explaining uncommitted work exists
4. Note: Changes from other engineers (parallel work) are expected and fine - only verify YOUR previous work is committed

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
    {"content": "Update [task] status to [Active]", "status": "pending", "activeForm": "Updating [task] status to [Active]"},
    {"content": "Understand context and catch up efficiently", "status": "pending", "activeForm": "Understanding context and catching up"},
    {"content": "Study ALL rules in .claude/rules/{backend|frontend|end-to-end-tests}/", "status": "pending", "activeForm": "Studying all rules for my role"},
    {"content": "Research existing patterns for this [task] type", "status": "pending", "activeForm": "Researching existing patterns"},
    {"content": "Implement [task] [name of the [task] from request file]", "status": "pending", "activeForm": "Implementing [task]"},
    {"content": "Build and verify ALL translations complete with grep (frontend-engineer only)", "status": "pending", "activeForm": "Building and verifying translations"},
    {"content": "Run validation tools and fix all failures/warnings", "status": "pending", "activeForm": "Running validation tools"},
    {"content": "Test changes in Chrome DevTools and fix ALL network warnings and console errors with zero tolerance (frontend-engineer only)", "status": "pending", "activeForm": "Testing in Chrome DevTools and fixing all issues"},
    {"content": "Update [task] status to [Review]", "status": "pending", "activeForm": "Updating [task] status to [Review]"},
    {"content": "Call reviewer subagent (only after all validation tools pass)", "status": "pending", "activeForm": "Calling reviewer subagent"},
    {"content": "MANDATORY: Call CompleteWork after reviewer approval to signal completion", "status": "pending", "activeForm": "Calling CompleteWork to signal completion"}
  ]
}
```

After creating base todo, expand "Implement [task]" with subtasks from [story] (if applicable).

---

## Workflow Steps

**STEP 2**: Study ALL rules for your role (read files or recall from memory)

- **Backend engineer**: ALL files in `.claude/rules/backend/`
- **Frontend engineer**: ALL files in `.claude/rules/frontend/`
- **QA engineer**: ALL files in `.claude/rules/end-to-end-tests/`

**STEP 3**: Research similar implementations in codebase

**STEP 4**: Implement each subtask, use **build** and **test** MCP tools continously

**STEP 5**: Frontend only - build and verify ALL translations complete

1. Run build to extract new translation strings to `*.po` files
2. Find ALL empty translations: `grep -r 'msgstr ""' */WebApp/shared/translations/locale/*.po`
3. Translate EVERY empty msgstr found (all languages: da-DK, nl-NL, etc.)
4. Use consistent domain terminology (check existing translations for guidance)

**STEP 6**: Run validation tools and fix all failures/warnings

For **backend [tasks]**, first run **build**, then run **format**, **test**, and **inspect** in parallel using the Task tool:
- Spawn three `backend-tool-runner` subagents simultaneously
- One runs `format`, one runs `test`, one runs `inspect`
- Wait for all three to complete
- Fix any failures or warnings (all must pass)

**Parallel execution example**:
```
In a single message, use Task tool three times:
1. Task tool → backend-tool-runner: "Run backend tool: format"
2. Task tool → backend-tool-runner: "Run backend tool: test"
3. Task tool → backend-tool-runner: "Run backend tool: inspect"
```

For **frontend [tasks]**, first run **build**, then run **format** and **inspect** MCP tools directly in parallel.

**STEP 7**: Frontend only - test changes in Chrome DevTools with ZERO TOLERANCE

**MANDATORY FOR FRONTEND ENGINEER - DO NOT SKIP**

1. **Navigate to https://localhost:9000** and test the changes:
   - Test all functionality that was implemented
   - Verify UI components render correctly
   - Test user interactions (clicks, forms, navigation, etc.)
   - If the website is not responding, use the **watch** MCP tool to restart the server (restarts .NET Aspire and runs database migrations in background)

2. **Monitor Network tab** - Fix ALL issues:
   - **Zero tolerance**: No failed requests, no 4xx/5xx errors
   - No slow requests without explanation
   - Fix ANY network warnings or errors (even if pre-existing per Boy Scout rule)

3. **Monitor Console tab** - Fix ALL issues:
   - **Zero tolerance**: No console errors, no warnings
   - Fix ANY console errors or warnings (even if pre-existing per Boy Scout rule)
   - Clear console and verify it stays clean during all interactions

4. **Login instructions**:
   - Username: `admin@platformplatform.local`
   - Use `UNLOCK` for verification code (works on localhost only)
   - If user doesn't exist: Sign up for a new tenant, use `UNLOCK` for verification code

**Boy Scout Rule**: Leave the codebase cleaner than you found it. If you see pre-existing console errors or network warnings unrelated to your changes, FIX THEM. Zero tolerance means ZERO - not "only for my changes".

**STEP 8**: Update [task] for review

**If `storyId` is not "ad-hoc":**
1. **Update [task] description** to reflect what was actually implemented:
   - If implemented exactly as described: Check off all subtask checkboxes `[x]`
   - If deviated from plan: Update description to document what was actually done

2. **Update [task] status to [Review]** in `[PRODUCT_MANAGEMENT_TOOL]`

**If `storyId` is "ad-hoc":**
- Skip [PRODUCT_MANAGEMENT_TOOL] operations
- Proceed directly to calling reviewer

**STEP 9**: Delegate to reviewer subagent to review and commit your code

**CRITICAL - Before calling reviewer**:

1. Run `git status --porcelain` to see ALL changed files
2. Identify YOUR files (files you created/modified for THIS task):
   - Backend: Include *.Api.json files (even though in WebApp folder - generated from your API changes)
   - Frontend: Exclude *.Api.json files (these belong to backend, not you)
   - Don't forget .po translation files
   - Exclude files from parallel engineers (different agent types)
   - If you changed files outside your scope: `git restore <file>` to revert
3. List YOUR files in "Files Changed" section (one per line with status)

**Delegation format**:
```
[One short sentence: what you implemented or fixed]

## Files Changed
- path/to/file1.tsx
- path/to/file2.cs
- path/to/translations.po

Request: {requestFilePath}
Response: {responseFilePath}
```

**MCP call parameters**:
- `agentType`: backend-reviewer, frontend-reviewer, or qa-reviewer
- `taskTitle`: From current-task.json
- `markdownContent`: Your delegation message above
- `branch`: From current-task.json
- `storyId`: From current-task.json
- `taskId`: From current-task.json
- `requestFilePath`: From current-task.json
- `responseFilePath`: From current-task.json

**Review loop**:
- If reviewer returns NOT APPROVED → Fix issues → Call reviewer subagent again
- If reviewer returns APPROVED → Check YOUR files (not parallel engineers' files) are committed → Proceed to completion
- **NEVER call CompleteWork unless reviewer approved and committed your code**
- **NEVER commit code yourself** - only the reviewer commits
- ⚠️ **If rejected 3+ times with same feedback despite validation tools passing:** Report problem with severity: error, then STOP COMPLETELY. No workarounds, no proceeding, no commits - just STOP and wait for human intervention.

**STEP 10**: Re-read [story], update plan if needed

**If `storyId` is not "ad-hoc":**
- Re-read [story] to check if there are more [tasks]
- Update plan if needed

**If `storyId` is "ad-hoc":**
- Skip (no [story] to re-read)

**STEP 11**: Signal completion and exit

⚠️ **CRITICAL - SESSION TERMINATING CALL**:

After completing all work AND receiving reviewer approval, you MUST call the MCP **CompleteWork** tool with `mode: "task"` to signal completion. This tool call will IMMEDIATELY TERMINATE your session - there is no going back after this call.

ALWAYS call CompleteWork after reviewer approval, even if this is the last task in a story.

**Before calling CompleteWork**:
1. Ensure all work is complete and all todos are marked as completed
2. Write a comprehensive response (what you accomplished, notes for Tech Lead)
3. Create an objective technical summary in sentence case (like a commit message)
4. Reflect on your experience and write categorized feedback using prefixes:
   - `[system]` - Workflow, MCP tools, agent coordination, message handling
   - `[requirements]` - Requirements clarity, acceptance criteria, task description
   - `[code]` - Code patterns, rules, architecture guidance

   Examples:
   - `[system] CompleteWork returned errors until title was less than 100 characters - consider adding format description`
   - `[requirements] Task mentioned Admin but unclear if TenantAdmin or WorkspaceAdmin`
   - `[code] No existing examples found for implementing audit logging in this context`

   You can provide multiple categorized items. Use report_problem for urgent system bugs during work.

**Call MCP CompleteWork tool**:
- `mode`: "task"
- `agentType`: Your agent type (backend-engineer, frontend-engineer, or qa-engineer)
- `taskSummary`: Objective technical description of what was implemented (imperative mood, sentence case). Examples: "Add user role endpoints with authorization", "Implement user avatar upload", "Fix null reference in payment processor". NEVER use subjective evaluations like "Excellent implementation" or "Clean code".
- `responseContent`: Your full response in markdown
- `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes as described above

⚠️ Your session terminates IMMEDIATELY after calling CompleteWork

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy the JSON from STEP 0**

**❌ DON'T: Create custom todo format**
