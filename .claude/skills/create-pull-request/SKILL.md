---
name: create-pull-request
description: Create a GitHub pull request.
allowed-tools: *
---

# Create Pull Request Workflow

## STEP 1: Gather Context

Run in parallel (quick reads, not background):

- `git status` - working tree state
- `git branch --show-current` - local branch
- `git --no-pager log --format=%s --reverse $(git merge-base HEAD main)..HEAD` - commits on the branch
- `git --no-pager diff main` - full diff
- `git rev-parse @{u} 2>/dev/null` - upstream presence (already pushed?)
- `gh pr view --json number,url,state 2>/dev/null` (only if upstream exists) - check if the branch already has an open pull request

## STEP 2: Kick Off Background Work

Everything in this step starts in the background (`run_in_background: true`) so the workflow proceeds without waiting. Results are reconciled in STEP 10.

### Validation

Skip anything already green this session. Only run what's genuinely uncertain.

When validation IS needed:

Run `build` first:
- **Backend change:** `build` (no flags - covers backend AND frontend; backend changes can break the frontend build via API contracts)
- **Frontend-only:** `build --frontend`
- **CLI-only:** `build --cli`

After build, in parallel (with `--no-build` where applicable):

- `aspire-restart` - service / Aspire wiring changed
- `format --<target>`, `lint --<target>` - for each target whose code changed
- `test --no-build` - backend change
- `e2e` - frontend or shared E2E-covered code changed; use `--smoke` for low-impact changes (judgment)

Use judgment to decide which target a file belongs to (e.g. project / manifest / lock files affect their owning build, even when the extension isn't obvious). Each item runs only if needed and not already green.

## STEP 3: Verify Branch Name

Three checks - all read-only at this stage; renames happen in STEP 8.

1. **Strip `worktree-` prefix.** Local-only convention; never push it.
2. **Naming convention.** Lowercase kebab-case.
3. **Drift check.** If the slug no longer describes the work (e.g. `fix-login-bug` whose commits do a refactor), draft a new name.

If either check 1 or check 3 fires, stash the proposed new name; STEP 4 will ask the user to confirm.

## STEP 4: Ask the User

Ask everything upfront so the user doesn't wait through slow research/drafting just to be prompted at the end. Single AskUserQuestion call, up to four questions:

1. **Labels** (multi-select, can pick zero or more): `Enhancement`, `Bug`, `Deploy to Staging`.
2. **Pull request checklist** (single-select): the recommended option spells out every `[ ]` item from `.github/PULL_REQUEST_TEMPLATE.md` in present-tense (e.g. for the current template: "Confirm you have added tests, done manual regression tests, and updated the documentation"). The alternative is "Leave checklist items unchecked".
3. **Review before pushing** (single-select): "Show me the rendered title and description before pushing" / "Push immediately".
4. **Branch rename confirmation** (single-select), only if STEP 3 flagged a rename: "Rename `<current>` to `<proposed>`?" with options Yes / No / Use a different name (free-text).

Stash the answers; consumed in STEP 6, 7, 8, 11.

## STEP 5: Correlate to [Task]

Look up `.claude/reference/product-management/[PRODUCT_MANAGEMENT_TOOL].md` for [Task] ID format and MCP tools.

1. **Parse [Task] ID from branch name** (e.g. `pp-(\d+)` for PlatformPlatform; strip `worktree-` first).

2. **If [Task] detected**, fetch via MCP. Use its title, description, and parent [Feature] to validate the draft in STEP 6 and spot drift.

3. **Detect unrelated commits (rare).** If some commits clearly belong to a different scope, offer to create a **new [Task]** as future follow-up - written as a fresh problem statement (future tense, what needs to happen). [Feature] choice: parent [Feature] of current branch's [Task] -> [Feature] with [Active] status -> ask.

4. **If no [Task] in the branch name**, ask: "No [Task] detected. Create one for this pull request?". If yes, draft a [Task] from the diff (problem statement, not what was done) and create it via MCP. Suggest a [Feature] using the same rules as above.

## STEP 6: Draft Title and Description

### Title

Write a fresh title based on the actual diff, commits, and [Task] context - not the branch name (branch names are often abbreviated and won't make a good title).

- Imperative form ("Fix", "Add", "Upgrade", "Refactor")
- Sentence case (not Title Case)
- No trailing period
- No "Pull Request" / "PR" / abbreviations
- No personal pronouns ("we", "our", "he", "she")
- No [Task] IDs in the title

### Description

Use the official template at `.github/PULL_REQUEST_TEMPLATE.md`:

- **Summary & Motivation**: lead with the most important change, explain what and why (not how), use bullets for multiple items, mention minor fixes last. **Do not end with a closing benefit summary** - that is the most common rejection reason.
- **No "pull request" or "pull-request" in the body** - CI rejects either phrase. Use "this change", "the change", etc.
- **Checklist**: copy items from the template. Render every `[ ]` as `[x]` if the user picked "Mark all checklist items..." in STEP 4; otherwise leave them as `[ ]`.

Save the rendered title (as a level-1 heading) + body to `.workspace/<branch-name>/pull-request.md`. Use the un-prefixed branch name if STEP 3 stripped `worktree-`.

## STEP 7: Optional Review Gate

If the user picked "Show me before pushing" in STEP 4:

- Print the rendered title and the body inline.
- Ask "Open pull request with this content? (yes / edit)".
- On "edit", let the user dictate changes. Re-save the markdown. Re-confirm.

Otherwise skip this step.

## STEP 8: Branch Hygiene

If STEP 4 confirmed a rename, rename the branch and verify. Safe in a worktree (directory path is independent of branch name). The push step sets the new upstream.

## STEP 9: Push

1. Re-check upstream tracking with `git rev-parse @{u} 2>/dev/null`.
2. **If unset** (branch never pushed, or just renamed): ask "Branch is local-only. Push to origin?". On confirm, push and ensure the pushed branch has upstream tracking (`git push --set-upstream origin "$(git branch --show-current)"`).
3. **If upstream exists**, check divergence:
   - `git log --oneline @{u}..HEAD` (local-ahead)
   - `git log --oneline HEAD..@{u}` (remote-ahead)
   - If remote-ahead: surface the divergence and stop. Don't force-push without an explicit user request.
   - If only local-ahead: `git push`.

## STEP 10: Wait for Background Validation

Check the status of the background tasks started in STEP 2.

- **Done and passed**: continue silently.
- **Done and failed**: print the failure and pause. Do not push or open the pull request until the user decides (fix / push anyway / abort).
- **Still running**: surface the status. Ask "Wait for completion, or proceed?". If proceeding, mention that CI will re-run these.
- **Killed earlier (user opt-out)**: skip.

## STEP 11: Create or Update the Pull Request via gh

Pre-flight: `gh auth status`. If not authenticated, surface and stop.

Use the existing pull request check result from STEP 1.

**If an open pull request already exists for this branch:**

```sh
gh pr edit <number> \
  --title "<title>" \
  --body-file ".workspace/<branch>/pull-request.md" \
  --add-label "<each-selected-label>"
```

(Strip the level-1 heading from the body file before passing.) Tell the user the pull request was updated, not newly created. Don't change draft/ready state without an explicit ask.

**If no pull request exists:**

```sh
gh pr create \
  --title "<title>" \
  --body-file ".workspace/<branch>/pull-request.md" \
  --base main \
  --assignee @me
```

Append `--label "<name>"` per label from STEP 4. `@me` resolves to the authenticated user. Don't pass `--draft`; this workflow opens ready-for-review.

Capture the pull request URL from gh's output for STEP 12.

## STEP 12: Report

Print:

- The pull request URL (clickable)
- Title
- Labels applied
- [Task] linked via branch name (auto by [PRODUCT_MANAGEMENT_TOOL])
- Pull-request markdown saved at the absolute path

End with a clickable link to the saved file.

## Examples

### Example 1 - Title

```
# DO: imperative, sentence case, no period
Add user profile image upload functionality
Fix data protection key sharing between self-contained systems
Upgrade dependency versions to latest stable releases

# DON'T: past tense, period, title case, prefixes
Added User Profile Image Upload Functionality.
PR: Implement new feature
Updating dependencies
```

### Example 2 - Description

```markdown
### Summary & Motivation

Add data protection key sharing between self-contained systems to fix antiforgery token validation failures. Previously, each self-contained system had isolated encryption keys, causing tokens generated in one system to be invalid in another.

- Configure a common application name for all self-contained systems
- Store keys in a user-accessible directory on disk

# DO: stop here - no closing benefit summary

### Checklist

- [x] I have added tests, or done manual regression tests
- [ ] I have updated the documentation, if necessary
```

```markdown
### Summary

# DON'T: personal pronouns, past tense, "pull request" term, vague descriptions
In this pull request we fixed a bug causing issues in our scs's.

- We added some configuration.
- Fixed a bug.

These changes make the system more robust and maintainable. # DON'T: closing summary
```

### Example 3 - Branch with `worktree-` prefix

Local: `worktree-pp-1208-add-pre-push-hook` -> propose `pp-1208-add-pre-push-hook` (rename in STEP 8, push in STEP 9).

### Example 4 - Branch drift

Local: `pp-1208-add-pre-push-hook`. Actual work: refactored install command and added auto-sync.

Propose `pp-1208-auto-install-git-hooks-on-pp-install`. Confirm in STEP 4; rename in STEP 8.

### Example 5 - Unrelated commits

Branch: `pp-1208-add-pre-push-hook`. Commits: hook + an unrelated typo fix in README.

Surface the typo commit. Offer to create a new [Task]: "Fix typo in README troubleshooting section" (problem statement, future tense). The pull request continues with both commits.

### Example 6 - End-of-output report

```
Pull request opened: https://github.com/<owner>/<repo>/pull/123

Title:        Auto-install committed git hooks into .git/hooks/ via pp install
Labels:       Enhancement, Deploy to Staging
[Task]:       linked via branch name pp-1208 (auto-detected by [PRODUCT_MANAGEMENT_TOOL])
Description:  /absolute/path/to/repo/.workspace/pp-1208-add-pre-push-hook/pull-request.md
```
