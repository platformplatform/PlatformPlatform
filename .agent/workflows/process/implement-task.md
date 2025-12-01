---
description: Implement a specific [task] from a [feature] following the systematic workflow
---
# Implement Task Workflow

You are implementing: **{{{title}}}**

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

- **Agentic mode**: The [taskId] comes from `current-task.json`, not from command arguments. The CLI passes only the [taskTitle] as the slash command argument. You run autonomously without human supervision - work with your team to find solutions.
- **Standalone mode**: Task details are passed as command arguments `{{{title}}}`. If a [taskId] is provided, read [feature] and [task] from `[PRODUCT_MANAGEMENT_TOOL]`. If no [taskId] provided, ask user to describe the task. There is no `current-task.json`.

## STEP 0: Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.agent/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
   - `requestFilePath`: Request file path
   - `featureId`: [FeatureId] (the feature this task belongs to, or "ad-hoc" for ad-hoc work)
   - `taskId`: [TaskId] (the task you're implementing, or "ad-hoc-yyyyMMdd-HHmm" for ad-hoc work)
   - `taskTitle`: Task title

   **If current-task.json does NOT exist:**

   This means there is no active task assignment. Call CompleteWork immediately to terminate your session:

   ```
   Call CompleteWork with:
   - mode: "task"
   - agentType: your agent type
   - taskSummary: "No active task assignment found"
   - responseContent: "Session invoked without active task. Current-task.json does not exist. Terminating session."
   - feedback: "[system] Session was invoked with /process:implement-task but no current-task.json exists - possible double invocation after completion"
   ```

   DO NOT proceed with any other work. DO NOT just say "nothing to do". Call CompleteWork immediately to terminate the session.

3. **Read the request file** from the path in `requestFilePath`.

4. **Verify Previous Work Committed**:

   Before proceeding, verify your previous task was committed:
   1. Run `git log --oneline -5` to check recent commits.
   2. Look for commits containing your agent type (e.g., "backend-engineer", "frontend-engineer").
   3. If your previous task is uncommitted: **REFUSE to start** and respond with error explaining uncommitted work exists.
   4. Note: Changes from other engineers (parallel work) are expected and fine - only verify YOUR previous work is committed.

5. **Create Todo List**

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Read [task] from [PRODUCT_MANAGEMENT_TOOL] and update status to [Active]", "status": "pending", "activeForm": "Reading task and updating status to Active"},
    {"content": "Understand the full feature context", "status": "pending", "activeForm": "Understanding feature context"},
    {"content": "Research existing patterns for this [task] type", "status": "pending", "activeForm": "Researching existing patterns"},
    {"content": "Implement each subtask", "status": "pending", "activeForm": "Implementing subtasks"},
    {"content": "Build and verify translations (frontend-engineer only)", "status": "pending", "activeForm": "Building and verifying translations"},
    {"content": "Run validation tools and fix all failures/warnings", "status": "pending", "activeForm": "Running validation tools"},
    {"content": "Test in browser with zero tolerance (frontend-engineer only)", "status": "pending", "activeForm": "Testing in browser"},
    {"content": "Fix any bugs discovered during validation/testing", "status": "pending", "activeForm": "Fixing bugs discovered"},
    {"content": "Update [task] status to [Review] and delegate to reviewer subagent (skip in standalone mode)", "status": "pending", "activeForm": "Updating status and calling reviewer"},
    {"content": "Check feature progress (skip in standalone mode/optional in agentic mode)", "status": "pending", "activeForm": "Checking feature progress"},
    {"content": "MANDATORY: Call CompleteWork after reviewer approval (skip in standalone mode)", "status": "pending", "activeForm": "Calling CompleteWork"}
  ]
}
```

**After creating this template**: Remove todo items marked for a different engineer role. For example, if you're a backend-engineer, remove items containing "(frontend-engineer only)".

---

## Workflow Steps

**STEP 1**: Read [task] from [PRODUCT_MANAGEMENT_TOOL] and update status to [Active]

**If `featureId` is NOT "ad-hoc" (regular task from a feature):**
1. Read [feature] from `featureId` in [PRODUCT_MANAGEMENT_TOOL] to understand the full PRD context
2. Read [task] from `taskId` in [PRODUCT_MANAGEMENT_TOOL] to get task details and subtask bullets
3. **Update [task] status to [Active]** in `[PRODUCT_MANAGEMENT_TOOL]`
4. **If [task] lookup fails** (not found, already completed, or error): This is a coordination error. Report a problem and call CompleteWork explaining the task could not be found.

**If `featureId` is "ad-hoc" (ad-hoc work):**
- Skip [PRODUCT_MANAGEMENT_TOOL] operations
- Still follow full engineer → reviewer → commit cycle

**After reading [task], unfold subtasks in todo:**

1. Extract the subtask bullets from [task] description.
2. Replace the "Implement each subtask" todo item with:
   - The task name as a parent item.
   - Each subtask as an indented child item (using ├─ and └─ formatting).

**Example:**
If task with title "Backend for user CRUD operations" has subtasks:
```
- Create UserId strongly typed ID
- Create User aggregate
- Create IUserRepository interface and implementation
- Create API endpoint for create user
```

Replace the single "Implement each subtask" item with:
```
Backend for user CRUD operations
├─ Create UserId strongly typed ID [pending]
├─ Create User aggregate [pending]
├─ Create IUserRepository interface and implementation [pending]
└─ Create API endpoint for create user [pending]
```

**STEP 2**: Understand the full feature context

Before implementing, understand the big picture:

1. **Read the [feature] from `featureId`** in [PRODUCT_MANAGEMENT_TOOL] (if not ad-hoc):
   - Understand the overall problem being solved and how the proposed solution will solve it.
   - Read the full PRD to understand business context.

2. **Read ALL [task] titles** (not full descriptions) in the [feature] (if not ad-hoc):
   - See the planned approach and implementation sequence.
   - Understand what you're building in context of the [feature].

3. **Read YOUR [task] description carefully**:
   - Already read in STEP 1, but review the subtask bullets.
   - Tasks are complete vertical slices.
   - Subtasks are already unfolded in your todo list (see STEP 1 above).

The [feature] plan was AI-generated by tech-lead in a few minutes after interviewing the user. You have implementation time to consider the code carefully. You are the expert closest to the code. If something doesn't align with:
- Feature intent.
- Rules in the project.
- Patterns used in the solution.
- Architectural patterns.
- Best practices.
- Simpler approaches.

**Question it.** Use report_problem or comment on the [task]. Better ideas from implementation phase should surface.

**Collaborate with your team**: For complex problems or architectural decisions, engage in conversation with team members (use ad-hoc delegation to discuss with other engineers). Better solutions often emerge from team collaboration.

**Note**: All architectural rules for your role are embedded in your system prompt and available for reference at all times.

**STEP 3**: Research existing patterns for this [task] type

Research the codebase to find similar implementations. Look for existing code that handles similar features, patterns, or business logic that can guide your implementation.

**STEP 4**: Implement each subtask

**Incremental development approach:**

Since [tasks] are complete vertical slices, build and test incrementally as you work through each subtask. This prevents accumulating errors and makes debugging easier.

**For EACH subtask in your todo:**

1. **Mark subtask [in_progress]** in todo.
2. **Implement the subtask**.
3. **Build immediately**:
   - Backend: `execute_command(command: "build", backend: true, selfContainedSystem: "{self-contained-system}")`.
   - Frontend: `execute_command(command: "build", frontend: true, selfContainedSystem: "{self-contained-system}")`.
   - Fix any build errors before proceeding.
4. **Test immediately** (backend only):
   - `execute_command(command: "test", backend: true, selfContainedSystem: "{self-contained-system}")`.
   - Fix any test failures before proceeding.
5. **Mark subtask [completed]** in todo.
6. **Move to next subtask**.

**Why build/test after each subtask:**
- Catches errors early when context is fresh.
- Prevents error accumulation.
- Makes debugging faster.
- Ensures each piece works before moving on.
- Critical for larger tasks.

**Do NOT run format/inspect after each subtask** - these are slow and run once at the end in STEP 6.

**STEP 5**: Build and verify translations (frontend-engineer only)

1. Run build to extract new translation strings to `*.po` files.
2. Find ALL empty translations: `grep -r 'msgstr ""' */WebApp/shared/translations/locale/*.po`.
3. Translate EVERY empty msgstr found (all languages: da-DK, nl-NL, etc.).
4. Use consistent domain terminology (check existing translations for guidance).

**STEP 6**: Run validation tools and fix all failures/warnings

**Zero tolerance for issues**:
- We deploy to production after review - quality is non-negotiable.
- **Boy Scout Rule**: Leave the codebase cleaner than you found it.
- Fix all failures, warnings, or problems anywhere in the system.
- This includes pre-existing issues unrelated to your changes.
- Don't request review with outstanding issues.

**Inspect findings block merging**: If inspect returns "Issues found", the CI pipeline will fail and the code cannot be merged. The severity level (note/warning/error) is irrelevant - all findings must be fixed before requesting review.

For **backend [tasks]**:
1. Run **inspect** for your self-contained system: `execute_command(command: "inspect", backend: true, selfContainedSystem: "{self-contained-system}")`.
2. Fix ALL failures found (zero tolerance).

**Note**: Build and test were already run after each subtask in STEP 4. Backend-engineer does NOT run format - the reviewer will handle formatting before commit.

For **frontend [tasks]**:
1. Run **build** for your self-contained system: `execute_command(command: "build", frontend: true, selfContainedSystem: "{self-contained-system}")`.
2. Run **format** for all self-contained systems: `execute_command(command: "format", frontend: true)`.
3. Run **inspect** for all self-contained systems: `execute_command(command: "inspect", frontend: true)`.
4. Fix ALL failures found (zero tolerance).

**STEP 7**: Test in browser with zero tolerance (frontend-engineer only)

**Required for frontend engineers**

1. **Navigate to https://localhost:9000** and test ALL functionality:
   - **Test the COMPLETE happy path** of the new feature from start to finish.
   - **Test ALL edge cases**: validation errors, empty states, maximum values, special characters.
   - **Test user scenarios**: What would a user actually do with this feature?
   - **Take screenshots** and critically examine if everything renders with expected layout and styling.
   - Test in **dark mode** and **light mode** (switch theme and verify UI renders correctly).
   - Test **localization** (switch language if feature has translatable strings).
   - Test **responsive behavior**: mobile size, small browser, large browser (resize and verify layout adapts).
   - Verify UI components render correctly (spacing, alignment, colors, borders, fonts).
   - Test all user interactions (clicks, forms, dialogs, navigation, keyboard navigation).
   - **Document what you tested** in your response (which scenarios, which user flows, which modes tested).
   - If website not responding, use **watch** MCP tool to restart server.

2. **Test with different user roles** (if applicable):
   - Test as admin user: `admin@platformplatform.local` / `UNLOCK`.
   - Test as non-admin user if feature has role-based access.
   - Verify permissions and access controls work correctly.

3. **Monitor Network tab** - Fix ALL issues:
   - **Zero tolerance**: No failed requests, no 4xx/5xx errors.
   - Check ALL API calls for the new feature execute successfully.
   - No slow requests without explanation.
   - Fix ANY network warnings or errors (even if pre-existing per Boy Scout rule).

4. **Monitor Console tab** - Fix ALL issues:
   - **Zero tolerance**: No console errors, no warnings.
   - Fix ANY console errors or warnings (even if pre-existing per Boy Scout rule).
   - Clear console and verify it stays clean during all interactions.

5. **Login instructions**:
   - Username: `admin@platformplatform.local`.
   - Use `UNLOCK` for verification code (works on localhost only).
   - If user doesn't exist: Sign up for a new tenant, use `UNLOCK` for verification code.

**Boy Scout Rule**: Leave the codebase cleaner than you found it. If you see pre-existing console errors or network warnings unrelated to your changes, FIX THEM. Zero tolerance means ZERO - not "only for my changes".

**STEP 8**: Fix any bugs discovered during validation/testing

If you discover bugs during testing or validation (API errors, broken functionality, console errors, broken UI, test failures), fix them before requesting review. Don't request review with known bugs.

**If bug is in existing code (not your changes)**:
1. Stash only your changes: `git stash push -- <your-files>` (don't include changes from other engineers working in parallel).
2. Verify the bug exists on clean code.
3. **Agentic mode**: Fix yourself if within your specialty OR delegate to engineer subagent if outside your specialty (use "ad-hoc" taskId).
   **Standalone mode**: Fix it yourself or inform user that the bug requires different expertise.
4. Follow STEP 10 to delegate to reviewer and get the fix committed.
5. `git stash pop` to restore your changes and continue.

**If you see errors that might be from parallel engineer's changes**:
- Check `git log --oneline` to see recent commits and understand what parallel engineer is working on.
- If recent commits exist: Sleep 5 minutes, then re-test (parallel engineer may be fixing it).
- If issue persists after 10-15 minutes: Delegate to that engineer or fix yourself if within specialty.

**Valid Solutions When Stuck**:
- Fix the bug yourself if it's within your specialty (your role boundaries).
- Delegate to appropriate engineer if bug is outside your specialty (use start_worker_agent with ad-hoc taskId).
- **Revert your changes** if solution is too complex - revert all git changes, fix pre-existing problems first, then re-implement cleanly.

**STEP 9**: Update [task] status to [Review] and delegate to reviewer subagent (skip in standalone mode)

**Before calling reviewer (every time, including re-reviews)**:

**1. Update [task] status to [Review]** in [PRODUCT_MANAGEMENT_TOOL] (if featureId is NOT "ad-hoc"):
   - This applies to EVERY review request, not just the first one.
   - When reviewer rejects and moves status to [Active], you MUST move it back to [Review] when requesting re-review.
   - Skip this only for ad-hoc work (featureId is "ad-hoc").

**2. Zero tolerance verification**: Confirm ALL validation tools pass with ZERO failures/warnings. NEVER request review with ANY outstanding issues - we deploy to production after review.

**3. Identify your changed files**:
- Run `git status --porcelain` to see ALL changed files.
- Identify YOUR files (files you created/modified for THIS task):
  - **Backend engineers**: MUST include `*.Api.json` files. These are auto-generated TypeScript types from your C# API endpoints, placed in WebApp/shared/lib/api/ for frontend consumption, but owned by backend.
  - **Frontend engineers**: MUST exclude `*.Api.json` files (these belong to backend, not you).
  - Don't forget `.po` translation files.
  - Exclude files from parallel engineers (different agent types).
  - If you changed files outside your scope: `git restore <file>` to revert.
- **CRITICAL for backend engineers**: Check `git status` for any `*.Api.json` files and include them in your file list.
- List YOUR files in "Files Changed" section (one per line with status).

Delegate to reviewer subagent:

**Delegation format**:
```
[One short sentence: what you implemented or fixed]

## Files Changed
- path/to/file1.tsx
- path/to/file2.cs
- path/to/translations.po

Request: {requestFilePath}
Response: {responseFilePath}

[If working in parallel: Include parallel work notification from coordinator, e.g., "⚠️ Parallel Work: Frontend-engineer is working in parallel on {task-title}"]
```

**MCP call parameters**:
- `agentType`: backend-reviewer, frontend-reviewer, or qa-reviewer
- `taskTitle`: From current-task.json
- `markdownContent`: Your delegation message above
- `branch`: From current-task.json
- `featureId`: From current-task.json
- `taskId`: From current-task.json
- `requestFilePath`: From current-task.json
- `responseFilePath`: From current-task.json

**Review loop**:
- If reviewer returns NOT APPROVED → Fix issues → Update [task] status to [Review] → Call reviewer subagent again.
- If reviewer returns APPROVED → Check YOUR files (not parallel engineers' files) are committed → Proceed to completion.
- Don't call CompleteWork unless reviewer approved and committed your code.
- Don't commit code yourself - only the reviewer commits.
- If rejected 3+ times with same feedback despite validation tools passing: Report problem with severity: error, then stop. Don't call CompleteWork, don't proceed with work - the user will take over manually.

**STEP 10**: Check feature progress (skip in standalone mode/optional in agentic mode)

**If `featureId` is NOT "ad-hoc" (regular task from a feature):**
- Optionally check if there are more [tasks] remaining in the [feature].
- This helps provide context in your completion message.

**If `featureId` is "ad-hoc" (ad-hoc work):**
- Skip (no [feature] to check).

**STEP 11**: Call CompleteWork after reviewer approval (skip in standalone mode)

After completing all work and receiving reviewer approval, call the MCP **CompleteWork** tool with `mode: "task"` to signal completion. This tool call will terminate your session.

CompleteWork requires reviewer approval and committed code.

Call CompleteWork after reviewer approval, even if this is the last [task] in a [feature].

**Before calling CompleteWork**:
1. Ensure all work is complete and all todos are marked as completed.
2. Write a comprehensive response (what you accomplished, notes for Coordinator).
3. Create an objective technical summary in sentence case (like a commit message).
4. Reflect on your experience and write categorized feedback using prefixes:
   - `[system]` - Workflow, MCP tools, agent coordination, message handling.
   - `[requirements]` - Requirements clarity, acceptance criteria, task description.
   - `[code]` - Code patterns, rules, architecture guidance.

   Examples:
   - `[system] CompleteWork returned errors until title was less than 100 characters - consider adding format description`.
   - `[requirements] Task mentioned Admin but unclear if TenantAdmin or WorkspaceAdmin`.
   - `[code] No existing examples found for implementing audit logging in this context`.

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

**✅ DO: Copy the JSON from STEP 2**.

**❌ DON'T: Create custom todo format**.
