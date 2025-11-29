---
description: Commit session changes to git—uses context to identify files, asks user to select commit message
---

# Commit Workflow

## Workflow

Speed is critical—commit should be fast. Don't recommend slow commands unless the changes warrant it.

1. **Run `git status --porcelain`** to see changed files (do NOT run `git log` or `git diff` — use session context)
   - Note file states: staged (A/M/D in first column), unstaged (in second column), deleted files need `git add` too

2. **Determine files to commit**:
   - **Clean session** (no prior context): Include all changed files
   - **With session context**: Include only files you changed; exclude others
   - Auto-generated files: `*.Api.json` (backend), `*.po` (frontend)

3. **Offer validation commands** (optional question in same `AskUserQuestion`):
   - Always offer all relevant commands, but recommend the most appropriate
   - **AI rules only**: Recommend sync-ai-rules, skip build/format/inspect
   - **Frontend changes**: Always offer build, format, inspect
   - **Backend changes**: Always offer build, format, inspect (note: format/inspect are slow)
   - **Big changes**: Also offer end-to-end tests
   - Let user multi-select or skip all

4. **Ask ALL questions in ONE `AskUserQuestion` call** — minimize back-and-forth:
   - Always include commit message options (2-4 choices)
   - **Commit message format**: Imperative form, sentence case (capital letter), no trailing period, no co-author
   - **No prefixes**: Do NOT use `feat:`, `fix:`, `docs:`, `chore:`, or similar conventional commit prefixes
   - Messages should be descriptive—capture the full scope of changes
   - One line but elaborate enough to understand the change without reading the diff
   - Add contextual questions as needed (file selection, validation commands, actions for leftovers, etc.)

5. **Stage explicitly and commit**:
   - `git add <file1> <file2> ...` — never use `git add -A` or `git add .`

6. **Only ask follow-up questions if user selects "Other" or gives unexpected input**

---

⚠️ **CRITICAL**: Being asked to commit now does NOT grant permission for future autonomous commits. Never commit, amend, or revert without explicit user instruction each time.
