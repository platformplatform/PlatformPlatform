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

**STEP 1**: Understand the full feature context

Before reviewing, understand the big picture:

1. **Read the parent [feature]** of your [story] in [PRODUCT_MANAGEMENT_TOOL]
   - Understand the overall problem being solved and how the proposed solution will solve it

2. **Read ALL [story] descriptions** in the [feature]
   - See the planned approach and implementation sequence

3. **List [task] titles** (not descriptions) for your current [story]
   - Understand what engineer was building in context of the [story]

4. **Read engineer's request and response files** to understand what was implemented

**IMPORTANT**: The [feature] plan was AI-generated by tech-lead in a few minutes after interviewing the user. Engineers spend implementation time thinking deeply about the code. YOU are the expert reviewer. If implementation OR task design doesn't align with:
- Feature intent
- Rules in the project
- Patterns used in the solution
- Architectural patterns
- Best practices
- Simpler approaches

**Reject and provide guidance.** Better ideas from review phase should surface.

**Collaborate with your team**: For complex problems or design questions, engage in conversation with engineers or other reviewers. Better solutions often emerge from team collaboration.

**STEP 2**: Run validation (role-specific)

**CRITICAL - ZERO TOLERANCE FOR ANY ISSUES**:
- We deploy to production after review - quality is non-negotiable
- **Boy Scout Rule**: The codebase must be cleaner than before
- REJECT if ANY failures, warnings, or problems exist ANYWHERE in the system
- This includes pre-existing issues unrelated to engineer's changes
- NEVER approve code with ANY outstanding issues

**For backend-reviewer** (validates all self-contained systems to catch cross-self-contained-system breakage):

1. Run **build** first for all self-contained systems (backend AND frontend)
   - Use execute_command MCP tool: `command: "build"`
   - DO NOT run in parallel

2. Run **format**, **test**, **inspect** in parallel for all self-contained systems
   - Spawn three `parallel-tool-runner` subagents simultaneously
   - "Run command: format"
   - "Run command: test"
   - "Run command: inspect"
   - Wait for all to complete

3. REJECT if ANY failures found (zero tolerance)

**For frontend-reviewer** (validates frontend only):

1. Run **build** for frontend: `execute_command(command: "build", frontend: true)`

2. Run **format** for all self-contained systems: `execute_command(command: "format", frontend: true)`

3. Run **inspect** for all self-contained systems: `execute_command(command: "inspect", frontend: true)`

4. REJECT if ANY failures found (zero tolerance)

**For qa-reviewer** (validates e2e tests):

1. Run **build** for frontend: `execute_command(command: "build", frontend: true)`

2. Run **e2e** tests (run in background, monitor output)

3. REJECT if ANY failures found (zero tolerance)

**If validation fails with errors unrelated to engineer's changes**:
- Check `git log --oneline` for recent parallel engineer commits
- If recent commits exist: Sleep 5 minutes, re-run validation
- If issue persists: REJECT and ask engineer to fix pre-existing issue

**STEP 3**: Study ALL rules for your role (read files or recall from memory)

- **Backend reviewer**: ALL files in `.claude/rules/backend/`
- **Frontend reviewer**: ALL files in `.claude/rules/frontend/`
- **QA reviewer**: ALL files in `.claude/rules/end-to-end-tests/`

**STEP 4**: Frontend only - verify translations in `*.po` files

Check all `*.po` files for empty `msgstr ""` entries and inconsistent domain terminology. Reject if translations are missing or terminology differs from established usage elsewhere.

**STEP 5**: Frontend only - test changes in Chrome DevTools with ZERO TOLERANCE

**MANDATORY FOR FRONTEND REVIEWER - DO NOT SKIP**

1. **Navigate to https://localhost:9000** and test ALL functionality:
   - **Test the COMPLETE happy path** of the new feature from start to finish
   - **Test ALL edge cases**: validation errors, empty states, maximum values, special characters, boundary conditions
   - **Test user scenarios**: What would a user actually do with this feature? Try to break it.
   - **Take screenshots** and critically examine if everything renders with expected layout and styling
   - Test in **dark mode** and **light mode** (switch theme and verify UI renders correctly)
   - Test **localization** (switch language if feature has translatable strings)
   - Test **responsive behavior**: mobile size, small browser, large browser (resize and verify layout adapts)
   - Verify engineer documented what they tested - if not documented, REJECT
   - If website not responding, use **watch** MCP tool to restart server

2. **Test with different user roles** (CRITICAL):
   - Test as admin: `admin@platformplatform.local` / `UNLOCK`
   - **Test as non-admin user** if feature has role-based behavior
   - Verify permissions, access controls, and role-specific UI elements work correctly
   - REJECT if role-based features not tested with appropriate roles

3. **Monitor Network tab** - REJECT if ANY issues found:
   - **Zero tolerance**: No failed requests, no 4xx/5xx errors
   - Check ALL API calls for the new feature execute successfully
   - No slow requests without explanation
   - REJECT if ANY network warnings or errors found (even pre-existing per Boy Scout rule)

4. **Monitor Console tab** - REJECT if ANY issues found:
   - **Zero tolerance**: No console errors, no warnings
   - REJECT if ANY console errors or warnings found (even pre-existing per Boy Scout rule)
   - Clear console and verify it stays clean during all interactions

5. **Analyze screenshots for UI quality** (take screenshots of new UI):
   - Check spacing, sizing, alignment, borders match design patterns
   - Verify responsive behavior (resize browser, test mobile viewport)
   - Check color contrast, typography, visual hierarchy
   - REJECT if UI elements are misaligned, poorly spaced, or inconsistent
   - AI is bad at visual design - use your human judgment on screenshots

6. **Login instructions**:
   - Username: `admin@platformplatform.local`
   - Use `UNLOCK` for verification code (works on localhost only)
   - If user doesn't exist: Sign up for a new tenant, use `UNLOCK` for verification code

**CRITICAL**: If you discover bugs during testing (API errors, broken functionality, console errors, network errors), you MUST REJECT. Zero tolerance means REJECT on ANY issue found.

**Boy Scout Rule**: If you find pre-existing issues unrelated to engineer's changes, REJECT and require engineer to fix them. Zero tolerance means ZERO - not "only for my changes".

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

**CRITICAL for backend-reviewer**: ALWAYS verify OpenAPI JSON file is included:
- Check if `application/{self-contained-system}/WebApp/shared/lib/api/*.Api.json` exists in git status
- This *.Api.json file MUST be committed with backend changes

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