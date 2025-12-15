# Review Task Workflow

You are reviewing: **{{{title}}}**

**Agentic vs standalone mode:** Your system prompt will explicitly state if you are in *agentic mode*. Otherwise, assume *standalone mode* and skip steps marked "(skip in standalone mode)".

- **Agentic mode**: The review request comes from `current-task.json`. The CLI passes only the task title as the slash command argument. You run autonomously without human supervision - work with your team to find solutions.
- **Standalone mode**: Review request is passed as command arguments `{{{title}}}`. Read changed files from `git status` or user-provided list.

## Review Principles

**Devil's Advocate Mindset**: Your job is to validate the engineer's work by actively searching for problems. Look for inconsistencies, deviations, and potential issues.

**Zero Tolerance**: ALL findings must be fixed, regardless of severity. Never dismiss issues as "minor" or "not worth fixing". Every deviation from rules or established patterns must be addressed.

**Evidence-Based Reviews**: Every finding must be backed by:
1. Explicit rules from `.github/copilot/rules/` files, OR
2. Established patterns found elsewhere in the codebase (cite specific file:line examples), OR
3. Well-established ecosystem conventions (e.g., .NET interfaces prefixed with `I`)

Avoid subjective personal preferences.

**Line-by-Line Review**: Like GitHub PR reviews - comment ONLY on specific file:line combinations that have issues. NO comments on correct code. NO commentary on what was done well.

**Objective Language**: State facts about rule violations or pattern deviations. Reference specific rules or codebase examples. Avoid subjective evaluations or praise.

**Concise Communication**: Minimize token usage for the engineer. Focus only on what needs fixing.

---

## STEP 0: Mandatory Preparation

1. **Read [PRODUCT_MANAGEMENT_TOOL]-specific guide** at `/.github/copilot/rules/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to understand terminology, status mapping, ID format, and MCP configuration.

2. **Read `current-task.json` from `.workspace/agent-workspaces/{branch-name}/{agent-type}/current-task.json`** to get:
   - `requestFilePath`: Request file path (contains engineer's request message)
   - `responseFilePath`: Response file path (where you'll write your review outcome)
   - `featureId`: [FeatureId] (the feature this task belongs to, or "ad-hoc" for ad-hoc work)
   - `taskId`: [TaskId] (the task being reviewed, or "ad-hoc-yyyyMMdd-HHmm" for ad-hoc work)
   - `taskTitle`: Task title

3. **Read the request file** from the path in `requestFilePath`.

4. **Read all files referenced in the engineer's request** (implementation details, changed files, etc.).

5. **Create Todo List**

**CALL TodoWrite TOOL WITH THIS EXACT JSON - COPY AND PASTE**:

```json
{
  "todos": [
    {"content": "Read [feature] and [task] to understand requirements", "status": "pending", "activeForm": "Reading feature and task"},
    {"content": "Create checklist of all requirements from [task] description", "status": "pending", "activeForm": "Creating requirements checklist"},
    {"content": "Run validation tools in parallel (format, test, inspect)", "status": "pending", "activeForm": "Running validation tools"},
    {"content": "Verify translations (frontend-reviewer only)", "status": "pending", "activeForm": "Verifying translations"},
    {"content": "Test in browser with zero tolerance (frontend-reviewer only)", "status": "pending", "activeForm": "Testing in browser"},
    {"content": "Review changed files one-by-one", "status": "pending", "activeForm": "Reviewing files"},
    {"content": "Review high-level architecture", "status": "pending", "activeForm": "Reviewing architecture"},
    {"content": "Verify all requirements met with tests", "status": "pending", "activeForm": "Verifying requirements"},
    {"content": "If approved, commit changes (or reject if any issues found)", "status": "pending", "activeForm": "Committing changes or rejecting"},
    {"content": "Update [task] status to [Completed] or [Active]", "status": "pending", "activeForm": "Updating task status"},
    {"content": "MANDATORY: Call CompleteWork", "status": "pending", "activeForm": "Calling CompleteWork"}
  ]
}
```

**After creating this template**: Remove todo items marked for a different reviewer role. For example, if you're a backend-reviewer, remove items containing "(frontend-reviewer only)".

**After creating base todo, unfold "Review changed files one-by-one":**

1. Get list of changed files from engineer's request (NOT from git status).
2. Replace the single "Review changed files" item with individual file review items.
3. Use tree format (├─ and └─).

**Example:**
```
Review changed files one-by-one
├─ Read and review User.cs [pending]
├─ Read and review UserRepository.cs [pending]
├─ Read and review CreateUserCommand.cs [pending]
└─ Read and review UsersEndpoint.cs [pending]
```

---

## Workflow Steps

**STEP 1**: Read [feature] and [task] to understand requirements

1. **Read the [feature]** from `featureId` in [PRODUCT_MANAGEMENT_TOOL] (if not ad-hoc):
   - Understand the overall problem and solution approach.

2. **Read the [task]** from `taskId` in [PRODUCT_MANAGEMENT_TOOL]:
   - Read the task description carefully.
   - Note all subtask bullets (implementation steps).

3. **Read engineer's request and response files** to understand what was actually implemented.

**If [task] lookup fails** (not found, already completed, or error): This is a coordination error. Report a problem and reject the review explaining the task could not be found.

**STEP 2**: Create checklist of all requirements from [task] description

Extract ALL business rules, edge cases, and validations from task description:
   - What are the business rules? (uniqueness, permissions, constraints).
   - What validations are required?
   - What edge cases must be handled?
   - What should NOT be allowed?
   - What are the tenant isolation requirements?

**Example requirements checklist (focus on details, not obvious structure):**
```
Business rules and validations:
- [ ] Email must be unique within tenant (not globally).
- [ ] Email validation (valid format).
- [ ] Only Tenant Owners can create users.
- [ ] Full name max length ≤ 100 characters.
- [ ] Cannot delete last Owner in tenant.
- [ ] Soft delete (not hard delete).
- [ ] Tenant isolation (users scoped to tenant).
- [ ] Max 3 tenant owners on a tenant.
...

Edge cases and error handling:
- [ ] Test duplicate email rejection.
- [ ] Test invalid email format.
- [ ] Test non-owner attempting create (403 Forbidden).
- [ ] Test deleting last owner (should fail).
- [ ] Test name > 100 chars validation.
- [ ] Test creating user in different tenant (isolation).
...
```

This checklist focuses on non-obvious requirements that reviewers often miss.

4. **Read engineer's request and response files** to understand what was actually implemented.

The [feature] plan was AI-generated by tech-lead in a few minutes after interviewing the user. Engineers spend implementation time considering the code carefully. You are the expert reviewer. If implementation or task design doesn't align with:
- Feature intent.
- Rules in the project.
- Patterns used in the solution.
- Architectural patterns.
- Best practices.
- Simpler approaches.

**Reject and provide guidance.** Better ideas from review phase should surface.

**Collaborate with your team**: For complex problems or design questions, engage in conversation with engineers or other reviewers. Better solutions often emerge from team collaboration.

**STEP 3**: Run validation tools in parallel (format, test, inspect)

**Zero tolerance for issues**:
- We deploy to production after review - quality is non-negotiable.
- **Boy Scout Rule**: The codebase must be cleaner than before.
- Reject if any failures, warnings, or problems exist anywhere in the system.
- This includes pre-existing issues unrelated to engineer's changes.
- Don't approve code with outstanding issues.
- Infrastructure failures (MCP errors, tools fail) → Reject, report problem, do not approve.

**Inspect findings block merging**: If inspect returns "Issues found", the CI pipeline will fail and the code cannot be merged. The severity level (note/warning/error) is irrelevant - all findings must be fixed before approval.

**For backend-reviewer** (validates all self-contained systems to catch cross-self-contained-system breakage):

1. Run **build** first for all self-contained systems (backend AND frontend):
   - Use execute_command MCP tool: `command: "build"`.
   - DO NOT run in parallel.

2. Run **format**, **test**, **inspect** in parallel (run sequentially in standalone mode):
   - Spawn three `parallel-tool-runner` subagents simultaneously.
   - "Run command: format".
   - "Run command: test".
   - "Run command: inspect".
   - Wait for all to complete.

3. Handle validation results:
   - **If NO parallel work notification in request**: REJECT if ANY failures found (zero tolerance).
   - **If parallel work notification present** (e.g., "⚠️ Parallel Work: Frontend-engineer..."):
     - REJECT if backend failures found (Core/, Api/, Tests/, Database/).
     - IGNORE frontend failures (WebApp/) unless caused by backend API contract changes.
     - If frontend failures seem related to backend API changes: Note in rejection that frontend-engineer may need to adapt.

**For frontend-reviewer** (validates frontend only):

1. Run **build** for frontend: `execute_command(command: "build", frontend: true)`.

2. Run **format** for all self-contained systems: `execute_command(command: "format", frontend: true)`.

3. Run **inspect** for all self-contained systems: `execute_command(command: "inspect", frontend: true)`.

4. Handle validation results:
   - **If NO parallel work notification in request**: REJECT if ANY failures found (zero tolerance).
   - **If parallel work notification present** (e.g., "⚠️ Parallel Work: Backend-engineer..."):
     - REJECT if frontend failures found (WebApp/).
     - IGNORE backend failures (Core/, Api/, Tests/) unless caused by frontend breaking the API contract.
     - If backend failures seem related to API integration: Note in rejection.

**For qa-reviewer** (validates E2E tests):

1. Run **build** for frontend: `execute_command(command: "build", frontend: true)`.

2. Run **e2e** tests (run in background, monitor output).

3. REJECT if ANY failures found (zero tolerance).

**If validation fails with errors unrelated to engineer's changes**:
- Check `git log --oneline` for recent parallel engineer commits.
- If recent commits exist: Sleep 5 minutes, re-run validation.
- If issue persists: REJECT. Per Boy Scout Rule, the engineer is responsible for fixing ALL issues found, even pre-existing ones.

**Note**: All architectural rules for your role are embedded in your system prompt and available for reference at all times.

**STEP 4**: Verify translations (frontend-reviewer only)

Check all `*.po` files for empty `msgstr ""` entries and inconsistent domain terminology. Reject if translations are missing or terminology differs from established usage elsewhere.

**STEP 5**: Test in browser with zero tolerance (frontend-reviewer only)

**Required for frontend reviewers**

If infrastructure issues prevent testing: Try to recover (use run MCP tool to restart server, retry browser). If recovery fails, complete the rest of your review, then reject with all findings including the infrastructure issue. Report problem for infrastructure failures.

1. **Navigate to https://localhost:9000** and test ALL functionality:
   - **Test the COMPLETE happy path** of the new feature from start to finish.
   - **Test ALL edge cases**: validation errors, empty states, maximum values, special characters, boundary conditions.
   - **Test user scenarios**: What would a user actually do with this feature? Try to break it.
   - **Take screenshots** and critically examine if everything renders with expected layout and styling.
   - Test in **dark mode** and **light mode** (switch theme and verify UI renders correctly).
   - Test **localization** (switch language if feature has translatable strings).
   - Test **responsive behavior**: mobile size, small browser, large browser (resize and verify layout adapts).
   - Verify engineer documented what they tested - if not documented, REJECT.
   - If website not responding, use **run** MCP tool to restart server.

2. **Test with different user roles** (CRITICAL):
   - Test as admin: `admin@platformplatform.local` / `UNLOCK`.
   - **Test as non-admin user** if feature has role-based behavior.
   - Verify permissions, access controls, and role-specific UI elements work correctly.
   - REJECT if role-based features not tested with appropriate roles.

3. **Monitor Network tab** - REJECT if ANY issues found:
   - **Zero tolerance**: No failed requests, no 4xx/5xx errors.
   - Check ALL API calls for the new feature execute successfully.
   - No slow requests without explanation.
   - REJECT if ANY network warnings or errors found (even pre-existing per Boy Scout rule).
   - ✗ BAD: "500 error is backend problem" → REJECT ANYWAY.
   - ✗ BAD: "Network error unrelated to my changes" → REJECT ANYWAY.

4. **Monitor Console tab** - REJECT if ANY issues found:
   - **Zero tolerance**: No console errors, no warnings.
   - REJECT if ANY console errors or warnings found (even pre-existing per Boy Scout rule).
   - Clear console and verify it stays clean during all interactions.
   - ✗ BAD: "Warning unrelated to my code" → REJECT ANYWAY.
   - ✗ BAD: "HMR error, not my problem" → REJECT ANYWAY.

5. **Analyze screenshots for UI quality** (take screenshots of new UI):
   - Check spacing, sizing, alignment, borders match design patterns.
   - Verify responsive behavior (resize browser, test mobile viewport).
   - Check color contrast, typography, visual hierarchy.
   - REJECT if UI elements are misaligned, poorly spaced, or inconsistent.
   - AI is bad at visual design - use your human judgment on screenshots.

6. **Login instructions**:
   - Username: `admin@platformplatform.local`.
   - Use `UNLOCK` for verification code (works on localhost only).
   - If user doesn't exist: Sign up for a new tenant, use `UNLOCK` for verification code.

If you discover bugs during testing (API errors, broken functionality, console errors, network errors), reject. Zero tolerance means reject on any issue found.

**Boy Scout Rule**: If you find pre-existing issues unrelated to engineer's changes, REJECT and require engineer to fix them. Zero tolerance means ZERO - not "only for my changes".

**STEP 6**: Review changed files one-by-one

**Review files individually, not in bulk:**

For EACH file in your unfolded todo:
1. **Mark file [in_progress]** in todo.
2. **Read the ENTIRE file** using Read tool.
3. **Review line-by-line** against rules and patterns:
   - Does it follow architectural patterns? (check similar files in codebase).
   - Are there any rule violations or pattern deviations?
   - Document findings: cite specific file:line + rule/pattern violated.
4. **Update todo item with result and mark [completed]**:
   - If file has issues: Change to "Read and review FileName.cs (Issues found)".
   - If file is clean: Change to "Read and review FileName.cs (Approved)".
5. **Move to next file**.

**Example todo progression:**
```
☒ ├─ Read and review TeamEndpoints.cs (Approved)
☒ ├─ Read and review CreateTeam.cs (Issues found)
☐ ├─ Read and review DeleteTeam.cs
```

**Why one-by-one:**
- Ensures thorough review of each file.
- Prevents missing details in bulk reviews.
- Critical for larger tasks.

Play the devil's advocate, and reject if you find ANY small thing that is objectively not correct.

**STEP 7**: Review high-level architecture

After reviewing all individual files, step back and review the overall design:

1. **Verify the implementation approach** makes sense:
   - Are entities/aggregates designed correctly?
   - Do commands/queries follow CQRS patterns?
   - Are API contracts well-designed?
   - Does the UI architecture follow patterns (frontend)?

2. **Check cross-file consistency**:
   - Do all pieces work together correctly?
   - Are naming conventions consistent?
   - Is the data flow logical?

3. **Verify it solves the business problem**:
   - Does this implementation actually deliver what the [task] requires?
   - Are there simpler approaches?

Play the devil's advocate, and reject if you find ANY small thing that is objectively not correct.

**Update todo item:**
- Change to "Review high-level architecture (Approved)" or "(Issues found)".
- Mark as [completed].

**STEP 8**: Verify all requirements met with tests

**Go through your requirements checklist from STEP 1 systematically:**

For EACH business rule:
1. **Find the implementation** - Search the reviewed files for where this rule is enforced.
2. **Find the test** - Search test files for test covering this rule.
3. **Verify edge case coverage** - Does the test check boundary conditions, error paths?

**For EACH validation:**
1. **Verify it exists** - Is the validation implemented?
2. **Verify error message** - Does it return proper error response?
3. **Verify test coverage** - Is there a test proving it rejects invalid input?

**For EACH permission check:**
1. **Verify guard exists** - Is permission checked in command/endpoint?
2. **Verify correct roles** - Does it check the right role (Owner, Admin, Member)?
3. **Verify test coverage** - Is there a test proving unauthorized access is rejected (403)?

If any requirement is missing, not implemented correctly, or not tested, reject with specific gaps.

**Example verification:**
```
Requirements verification:
✓ Email unique within tenant - Implemented in User.cs:45, tested in CreateUserTests.cs:120.
✗ Only Owners can create - No permission guard found in CreateUserCommand.
✗ Cannot delete last Owner - Implementation exists in DeleteUserCommand.cs:67 but NO TEST.
✗ Tenant isolation - Tests only check happy path, missing test for cross-tenant access.

REJECT: Missing permission guard for create. Missing test for last-owner protection. Missing tenant isolation test.
```

**Update todo item:**
- Change to "Verify all requirements met with tests (Approved)" or "(Requirements missing)".
- Mark as [completed].

**STEP 9**: If approved, commit changes (or reject if any issues found)

**Aim for perfection, not "good enough".**

By this point, you've already marked each file, architecture, and requirements as "(Approved)" or "(Issues found)". Now make the final decision:

**APPROVED only if ALL criteria met:**
- ✓ All validation tools passed (build, format, test, inspect).
- ✓ Browser testing completed successfully (frontend only).
- ✓ Zero console errors or warnings.
- ✓ Zero network errors (no 4xx, no 5xx).
- ✓ No skipped mandatory steps for ANY reason.
- ✓ All code follows rules and patterns.
- ✓ Pre-existing issues fixed (Boy Scout Rule).
- ✓ All files marked "(Approved)".
- ✓ Architecture marked "(Approved)".
- ✓ Requirements marked "(Approved)".

**Reject if any issue exists - no exceptions. Common rationalizations to avoid:**
- ✗ "Backend issue, not frontend problem" → Reject anyway.
- ✗ "Previous review verified it" → Reject anyway.
- ✗ "Validation tools passed" → Not enough if browser has errors.
- ✗ "Infrastructure/MCP issue" → Reject anyway, report problem.
- ✗ "Pre-existing problem" → Reject anyway per Boy Scout Rule.
- ✗ "It's just a warning" → Reject, zero means zero.

**When rejecting:** Do full review first, then reject with ALL issues listed (avoid multiple rounds). Skip to STEP 9 to update status, then STEP 10 to call CompleteWork.

**If APPROVED, proceed with commit:**

1. Identify files to commit from review context:
   - Run `git status --porcelain` to see all changed files
   - Filter to YOUR scope only:
     - **Backend reviewer**: Api/Core/Tests files + `*.Api.json` files (auto-generated, in WebApp folder)
     - **Frontend reviewer**: WebApp files + `*.po` files (auto-generated) EXCEPT `*.Api.json` files
2. Stage files: `git add <file>` for each file
3. Commit: One line, imperative form, no description, no co-author
4. Get hash: `git rev-parse HEAD`

Don't use `git add -A` or `git add .`

**STEP 10**: Update [task] status to [Completed] or [Active]

**If `featureId` is NOT "ad-hoc" (regular task from a feature):**
- If APPROVED: Update [task] status to [Completed].
- If REJECTED: Update [task] status back to [Active].

**If `featureId` is "ad-hoc" (ad-hoc work):**
- Skip [PRODUCT_MANAGEMENT_TOOL] status updates.

**STEP 11**: Call CompleteWork

Call MCP **CompleteWork** tool with `mode: "review"` - your session terminates after this call.

**Categorized Feedback Required**:
Use category prefixes for all feedback:
- `[system]` - Workflow, MCP tools, agent coordination, message handling.
- `[requirements]` - Requirements clarity, acceptance criteria, task description.
- `[code]` - Code patterns, rules, architecture guidance.

Examples:
- `[system] Validation tools reported stale results from previous run`.
- `[requirements] Engineer's file list didn't match git status - unclear which files were in scope`.
- `[code] Missing examples for implementing telemetry in this pattern`.

**For APPROVED reviews**:
- Provide: `mode: "review"`.
- Provide: `commitHash` (from `git rev-parse HEAD` in STEP 8).
- Provide: `rejectReason` as null or empty string.
- Provide: `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes.

**For REJECTED reviews**:
- Provide: `mode: "review"`.
- Provide: `commitHash` as null or empty string.
- Provide: `rejectReason` (sentence case, imperative mood).
- Provide: `feedback`: Mandatory categorized feedback using [system], [requirements], or [code] prefixes.

---

## Response Format Requirements

When calling CompleteWork with `responseContent`:

**For REJECTED reviews**:

```markdown
[Short objective summary of why rejected - 1-2 sentences or short paragraph if more elaboration needed]

## Issues

### File.cs:Line
[Objective description of problem]
- **Rule/Pattern**: [Reference to .github/copilot/rules/X.md or pattern from codebase]
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

**Requirements**:
- Line-by-line review like GitHub PR.
- NO comments on correct code.
- NO subjective language ("excellent", "great", "well done").
- NO dismissing issues as "minor" or "optional".
- Cite specific rules or codebase patterns.
- Keep responses concise to minimize token usage.

---

## REMINDER: Use Exact TodoWrite JSON

**✅ DO: Copy JSON from above**.

**❌ DON'T: Create custom format**.
