---
name: pull-platformplatform-changes
description: Pull unmerged PlatformPlatform pull requests into a downstream project by cherry-picking each one onto the platformplatform-updates branch. Renames commits to "PlatformPlatform PR N - title" per the existing convention. Drives a ralph-loop that validates each commit (build, test, format, lint, optional e2e) before moving on. Use in downstream projects that have PlatformPlatform configured as the upstream remote.
allowed-tools: Read, Write, Edit, Bash, AskUserQuestion, EnterWorktree
---

# Pull PlatformPlatform Changes

Discover unported PlatformPlatform pull requests and emit a `/ralph-loop:ralph-loop` prompt that the user runs themselves to cherry-pick them one at a time. Optionally interviews the user upfront to capture porting decisions for each PR.

This skill assumes the current repo is a downstream project with `upstream` configured as the PlatformPlatform remote. The CLI command `pull-platformplatform-changes` will fail if the remote isn't set up — that's the expected guardrail.

## STEP 1: Detect resume vs. fresh start

Compute the root repo path:

```bash
ROOT_REPO=$(dirname "$(git rev-parse --path-format=absolute --git-common-dir)")
CURRENT_BRANCH=$(git branch --show-current)
```

A run is in progress if `$ROOT_REPO/.workspace/platformplatform-updates/commits.md` exists AND the current branch is `platformplatform-updates`.

**If resuming:**
1. Read `commits.md` and count `[x]` vs `[ ]` lines. Identify the next pending commit.
2. Read `learnings.md` and surface the last 3 entries.
3. Read `port-plan.md` if it exists and surface a brief summary of upcoming PR decisions.
4. Show the user a status panel: N of M PRs done, next PR + title, last learnings.
5. Use AskUserQuestion to confirm continue and offer a run-mode change.
6. Skip to STEP 6 with remaining iteration count.

**If fresh:** continue to STEP 2.

## STEP 2: Verify clean working tree

```bash
git status --porcelain
```

If there are uncommitted changes, stop and ask the user to stash or commit first.

## STEP 3: Discover unported PRs

```bash
dotnet run --project developer-cli -- pull-platformplatform-changes --list-only
```

Output is one line per unported PR: `<date> <hash> <subject> (#<pr>)`.

If the output is empty, stop and report nothing to port.

Parse each line into `{date, hash, subject, prNumber}`. The PR number is in the trailing `(#NNN)` group. The ported commit subject is `PlatformPlatform PR <prNumber> - <subject without (#<prNumber>)>`.

Show the list newest-first to the user (date + ported title) and confirm the range.

## STEP 4: Worktree decision

Use AskUserQuestion to ask whether to create a worktree. Some downstream projects don't support worktrees, so always ask.

**If yes:**
```bash
git worktree add .claude/worktrees/platformplatform-updates main
```
Then call the `EnterWorktree` tool to switch context.

**If no:** the existing CLI flow handles branch creation during cherry-pick. For the ralph-loop, create the branch now off main:
```bash
git switch --create platformplatform-updates main
```

## STEP 5: Run mode and planning files

Use AskUserQuestion to ask the run mode:
- **autonomous** — loop ports every PR best-effort, never asks. Logs unusual decisions to `learnings.md`.
- **interview-upfront** — skill walks each PR with the user before the loop runs, capturing decisions in `port-plan.md`. The loop reads `port-plan.md` for guidance on each commit.
- **interview-on-problem** — loop ports best-effort but pauses via AskUserQuestion when it can't make progress.

### If interview-upfront mode

For each unported PR (oldest first), examine its objective and decide if downstream needs adaptation:

1. Fetch the PR's title, description, and changed files:
   ```bash
   gh pr view <prNumber> --repo platformplatform/PlatformPlatform --json title,body,files
   ```
2. Read the PR body and the list of changed files. Decide whether this is:
   - **Routine** — bug fix, dependency bump, refactor that has no downstream impact. Mark for clean port.
   - **Likely adaptation** — touches an area where the downstream has diverged (e.g. component library, auth flow, telemetry). Use AskUserQuestion to ask the user how to handle it.
   - **Likely skip** — change targets a feature the downstream doesn't have, or downstream has drifted so far the PR is no longer applicable. Use AskUserQuestion to confirm skip with reason.

3. Write the decision to `port-plan.md` (see format below). Keep notes brief and action-oriented — the ralph-loop reads them per-commit.

Don't interview the user on every PR — only when the diff suggests a real adaptation question. Routine PRs can flow through without disruption.

### Always: write planning files in the root repo

```
$ROOT_REPO/.workspace/platformplatform-updates/commits.md
$ROOT_REPO/.workspace/platformplatform-updates/learnings.md
$ROOT_REPO/.workspace/platformplatform-updates/port-plan.md   (interview-upfront only)
```

**`commits.md` format:**
```markdown
<!-- mode: <run-mode> | source: pull-platformplatform-changes | started: <YYYY-MM-DD> -->
N PRs to port.

[ ] <hash> PlatformPlatform PR <N> - <subject>
[ ] <hash> PlatformPlatform PR <N> - <subject>
...
```

Lines are oldest-first. Subjects are already renamed to the ported convention.

**`learnings.md`** starts as just a heading:
```markdown
# Learnings — PlatformPlatform pull
```

**`port-plan.md` format (interview-upfront only):**
```markdown
# Port plan — PlatformPlatform pull

## PR <N> — <subject>
- decision: port-as-is | adapt | skip
- notes: <one to three lines of guidance for the ralph-loop>
```

## STEP 6: Emit the ralph-loop prompt

Compute iterations:
```
N = number of [ ] lines in commits.md (PRs marked skip in port-plan.md still count — the loop processes them)
iterations = max(ceil(N * 1.25), N + 10)
```

Send TWO separate messages.

**Message 1:**
```
Use /copy to copy the following ralph-loop and run it from your Claude Code terminal.
```

**Message 2 — the literal command, nothing else:**
```
/ralph-loop:ralph-loop "<PROMPT>" --max-iterations <N> --completion-promise "WE ARE DONE"
```

## Ralph-loop prompt template

Substitute `{{worktree-path}}`, `{{root-repo}}`, `{{commits-file}}`, `{{learnings-file}}`, `{{port-plan-file}}` (or omit the port-plan reference if not in interview-upfront mode), `{{run-mode}}`. Use real newlines.

```
We are pulling unmerged PlatformPlatform pull requests into the platformplatform-updates branch at {{worktree-path}} by cherry-picking commits one at a time from {{commits-file}}. Run mode: {{run-mode}}. Per-PR plan: {{port-plan-file}} (consult before each commit; absent means no upfront interview was done).

For each iteration:

1. Pick the next commit — the first line in the commits file without [x]. If every line is marked, output WE ARE DONE.

2. Consult the port plan — if {{port-plan-file}} exists, find the matching PR section and follow its decision and notes. If decision is 'skip', mark [x] with a [SKIPPED: <reason>] suffix and move on without cherry-picking.

3. Cherry-pick — `git cherry-pick -m 1 --strategy=recursive -X theirs <hash>`. Resolve any conflicts so the working tree reflects the intended state of the PR. Note whether conflicts were manually resolved — this drives the validation order in step 5. If the cherry-pick produces no changes (already applied downstream), skip the rest and mark [x] with [NO-OP: already applied].

4. Rename the commit — amend the commit message to 'PlatformPlatform PR <N> - <original subject without trailing (#<N>)>'. Strip the trailing PR number suffix; preserve the rest of the body. Strip any `# Conflicts:` block git inserted after a conflicted cherry-pick, and any other `#`-prefixed comment lines — PlatformPlatform's pull-request-conventions CI accepts multi-line messages only for `PlatformPlatform PR ...` and `Co-authored-by:` commits, but conflict-marker noise must never land in the final history.

5. Validate the change — order depends on whether conflicts were resolved in step 3:

   **Conflicts resolved (we touched code by hand):**
   1. build (must succeed)
   2. format (must run before lint, since manual edits often need formatting that would otherwise show up as lint findings)
   3. in parallel: backend tests, restart Aspire, e2e tests (smoke for small changes; full suite for large changes; full suite at least every 5 commits)
   4. lint
   5. if lint or any earlier step changed anything, restart this validation from step 5.1

   **No conflicts (clean cherry-pick):**
   1. build (must succeed)
   2. in parallel: backend tests, format, lint, restart Aspire, e2e tests (same scope rules as above)
   3. if lint or any other parallel step changed anything, restart this validation from step 5.1

   E2E run with --stop-on-first-failure --quiet to fail fast. If tests fail, do not move on until the full suite is green. If something is missing because a later PR introduces it, pull only the minimum needed lines forward and note it under that future commit in the commits file as an indented line.

6. Fold all fixes into the cherry-picked commit — amend so the commit is self-contained. Re-run the sanitize step from #4 if the amend re-introduces any `# Conflicts:` block or `#`-prefixed lines.

7. Verify the working tree is clean — git status must show zero changes after the amend.

8. Mark the commit done — change [ ] to [x] on its line in {{commits-file}}.

9. Log learnings — append a one-line entry to {{learnings-file}} for any non-trivial adaptation, conflict resolution choice, skipped PR, or NO-OP. Format: '- PR <N> — <one-line note>'. Routine clean ports do not need an entry.

Mode-specific behavior:
- autonomous: do not ask the user anything. Make every judgement call yourself. Note unusual choices in {{learnings-file}}.
- interview-upfront: follow {{port-plan-file}} closely. If you find the plan disagrees with reality (e.g. a 'port-as-is' PR has an unexpected conflict), use AskUserQuestion to clarify rather than guessing.
- interview-on-problem: when you cannot make progress (irreconcilable conflict, ambiguous adaptation, change that no longer applies), use AskUserQuestion to ask. Bake the answer into your next attempt and log it in {{learnings-file}}.

Operating principles:

- Never stop. If a PR cannot be ported as-is, adapt it: collapse it with a neighbor, split it, reorder it, drop changes that don't apply downstream, or skip it with a logged reason. The goal is a downstream that reflects PlatformPlatform's intent, not literal preservation of every commit.
- Smallest possible change when borrowing forward — pull only the lines required to compile or pass tests.
- Trust the loop. Every iteration ends with a clean tree, a passing build, passing tests, passing lint, passing e2e, and one more [x] in the commits file.
```
