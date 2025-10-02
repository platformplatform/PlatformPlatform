---
description: Workflow for review code changes for quality, patterns, and rule adherence
auto_execution_mode: 1
---

# Review Code Workflow

Review scope: $ARGUMENTS

Focus your review on the branch, file path, or scope provided in the arguments above. If no scope is provided, review the current uncommitted changes. Follow this workflow to review files *one by one*.

## Workflow

1. Determine review scope:
   - If the prompt provides a list of files or folders, use that list as the review scope.
   - If the repository has uncommitted changes, assume the current branch is the review scope. Use `git status --porcelain` and `git diff --name-only` to list all modified, staged, and untracked files, ensuring all uncommitted changes are included.
   - If the repository has no uncommitted changes, assume the current branch is the review scope. Use `git diff --name-only $(git merge-base HEAD main)..HEAD` to list all files changed in the branch.
   - Combine and deduplicate the file list for review.
   - Exclude all auto generated files (e.g. `*.Api.json`, `*.po`).

2. Inspect all *.md files in `.claude` and compile the rule list:
   - Read all *.md files in `.claude` and use their glob patterns to compile and memorize a list of rules that apply to the files being reviewed.
   - Memorize this list so it can be reused for each changed file, without re-reading all *.md files for each file.

3. Review backend files first, then frontend files:
   - Always review backend files before frontend files, as backend changes can break frontend functionality.
   - For each file, follow steps 4 and 5 before moving to the next file.
   - Do not batch review or fix multiple files at once.

4. Critically review the file:
   - For each file, determine which rules from the memorized list apply by matching the file path against the glob patterns.
   - Read the file in full and compare every aspect to the requirements in the relevant rules.
   - Be extremely critical and thorough: Check naming, spelling, formatting, line breaks, code style, conventions, structure, all rule-specific requirements, and all code comments for rule compliance.
   - Compile a detailed list of all issues, even minor ones, that do not conform to the rules.
   - Reference the specific rule(s) for each issue found.
   - Print the result to the chat.

5. Fix all problems in the file:
   - Apply fixes for every issue identified, ensuring the file fully conforms to all relevant rules.
   - Do not leave any known issues unresolved before moving to the next file.
   - If a fix is not obvious, reference the rule and provide a rationale for leaving it as-is.

6. After all backend files are reviewed and fixed:
   - Use the **check MCP tool** for backend to run build, format, test, and inspect. See [Tools](/.windsurf/rules/tools.md) for details.
   - Ensure all checks pass before proceeding to review frontend files.

7. After all frontend files are reviewed and fixed:
   - Use the **check MCP tool** for frontend to run build, format, test, and inspect.
   - Ensure all checks pass before completing the review process.

## Examples

### Example 1 - Review findings and fixes

```markdown
# ✅ DO: List all issues with references to rules
- The command record is not marked as `sealed`. ([backend.md](/.windsurf/rules/backend/backend.md))
- The validator uses different messages for the same property. ([commands.md](/.windsurf/rules/backend/commands.md))
- Telemetry event is not named in past tense. ([telemetry-events.md](/.windsurf/rules/backend/telemetry-events.md))

# ❌ DON'T: Be vague or skip minor issues
- Looks good overall, just a few nits.
- Only fixed the main bug, left formatting for later.
```

### Example 2 - File review and fix workflow

```bash
# Step 1: List files to review (current branch)
git diff --name-only $(git merge-base HEAD main)..HEAD

# Step 2: Inspect all *.md files in .claude and compile the rule list
# Step 3: Review backend files first, then frontend files
# Step 4: For each file, determine applicable rules, review, and print issues
# Step 5: Fix all issues in the file
# Step 6: After all backend files are reviewed and fixed:
#         Use the **check MCP tool** for backend
# Step 7: After all frontend files are reviewed and fixed:
#         Use the **check MCP tool** for frontend
```

### Example 3 - Referencing rules for issues

```markdown
- Property names are not in snake_case. ([telemetry-events.md](/.windsurf/rules/backend/telemetry-events.md))
- Used `DateTime.UtcNow()` instead of `TimeProvider.System.GetUtcNow()`. ([backend.md](/.windsurf/rules/backend/backend.md))
```