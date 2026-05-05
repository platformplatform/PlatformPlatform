---
name: rebuild-branch
description: Rebuild a stale branch by cherry-picking each commit onto a fresh branch off main, using a ralph-loop to validate each commit (build, test, format, lint, optional e2e) before moving on. Use when a branch has gone stale, has merge conflicts with main, or needs to be split out of a long-lived feature branch.
allowed-tools: Read, Write, Edit, Bash, AskUserQuestion, EnterWorktree
---

# Rebuild Branch

Drive the discovery + planning phase of a branch rebuild. Emit a `/ralph-loop:ralph-loop` prompt the user runs themselves to execute the cherry-picks one at a time.

## STEP 1: Determine source branch

The user may have named the source branch as the skill argument. If not, ask which branch to rebuild.

If the source branch isn't crystal-clear (multiple matches, ambiguous shorthand), confirm the resolved name with the user before proceeding.

## STEP 2: Detect resume vs. fresh start

Compute the root repo path (the main worktree, not the current one):

```bash
ROOT_REPO=$(dirname "$(git rev-parse --path-format=absolute --git-common-dir)")
CURRENT_BRANCH=$(git branch --show-current)
```

A run is in progress if `$ROOT_REPO/.workspace/$CURRENT_BRANCH/commits.md` exists AND the current branch ends with `-rebuild`.

**If resuming:**
1. Read `commits.md` and count `[x]` vs `[ ]` lines. Identify the next pending commit.
2. Read `learnings.md` and surface the last 3 entries.
3. Show the user a status panel: branch name, N of M commits done, next commit hash + title, last learnings.
4. Use AskUserQuestion to confirm continue and offer a run-mode change. Options: keep current mode, switch to autonomous, switch to interview-upfront, switch to interview-on-problem.
5. Skip to STEP 7 (emit ralph-loop) with remaining iteration count.

**If fresh:** continue to STEP 3.

## STEP 3: Verify clean working tree

```bash
git status --porcelain
```

If there are uncommitted changes, stop and ask the user to stash or commit first. Do not proceed.

## STEP 4: Discover commits

Determine the base. If currently on `main`, the base is `main`. Otherwise use AskUserQuestion to ask whether to base on `main` or on the current branch.

List commits oldest-first:

```bash
git log <base>..<source-branch> --reverse --format='%h %s'
```

Show the list newest-first to the user with hash + title and confirm the range. If empty, stop and report nothing to do.

## STEP 5: Worktree decision

Use AskUserQuestion to ask whether to create a worktree. (Some downstream projects don't support worktrees, so always ask — never assume.)

**If yes:**
```bash
git worktree add .claude/worktrees/<source-branch>-rebuild main
```
Then call the `EnterWorktree` tool to switch context. If the user chose "current branch" as the base in STEP 4, checkout the source branch inside the worktree:
```bash
git checkout <source-branch>
```

**If no:** create the rebuild branch in the current repo:
```bash
git switch --create <source-branch>-rebuild main
```

## STEP 6: Run mode and write planning files

Use AskUserQuestion to ask which run mode the loop should use:
- **autonomous** — loop plows through, fixes problems on its own, never asks
- **interview-upfront** — N/A for rebuild-branch (only meaningful for the pull skill); fall through to interview-on-problem if selected
- **interview-on-problem** — loop pauses and asks via AskUserQuestion when it can't make progress

Write the planning files in the **root repo** (not the worktree) so they survive worktree deletion:

```
$ROOT_REPO/.workspace/<source-branch>/commits.md
$ROOT_REPO/.workspace/<source-branch>/learnings.md
```

**`commits.md` format:**
```markdown
<!-- mode: <run-mode> | source: rebuild-branch | started: <YYYY-MM-DD> -->
N commits to apply.

[ ] <hash> <subject>
[ ] <hash> <subject>
...
```

Lines are oldest-first (the order they will be applied).

**`learnings.md`** starts as just a heading:
```markdown
# Learnings — <source-branch> rebuild
```

## STEP 7: Emit the ralph-loop prompt

Compute iterations:
```
N = number of commits remaining
iterations = max(ceil(N * 1.25), N + 10)
```

Send TWO separate messages (this is required so `/copy` only grabs the prompt, not the instructions).

**Message 1 — instructions:**
```
Use /copy to copy the following ralph-loop and run it from your Claude Code terminal.
```

**Message 2 — the literal command, nothing else:**
```
/ralph-loop:ralph-loop "<PROMPT>" --max-iterations <N> --completion-promise "WE ARE DONE"
```

Where `<PROMPT>` is the template below with placeholders filled in. Use real newlines, not escaped `\n`.

## Ralph-loop prompt template

Substitute `{{branch}}`, `{{worktree-path}}`, `{{root-repo}}`, `{{commits-file}}`, `{{learnings-file}}`, `{{run-mode}}`. If no worktree was created, `{{worktree-path}}` equals `{{root-repo}}`.

```
We are rebuilding the {{branch}} branch onto {{worktree-path}} by cherry-picking commits one at a time from {{commits-file}}. Run mode: {{run-mode}}.

For each iteration:

1. Pick the next commit — the first line in the commits file without [x]. If every line is marked, output WE ARE DONE.

2. Land the commit cleanly — cherry-pick it and resolve any conflicts so the working tree reflects the intended state of that commit. Note whether conflicts were manually resolved — this drives the validation order in step 4.

3. Sanitize the commit message — the message must be a single line (PlatformPlatform's pull-request-conventions CI forbids multi-line messages except for `Co-authored-by:` trailers). Strip any `# Conflicts:` block that git inserts after a conflicted cherry-pick, and any other `#`-prefixed comment lines. Verify with `git log -1 --format=%B` that the message is one line.

4. Validate the change — order depends on whether conflicts were resolved in step 2:

   **Conflicts resolved (we touched code by hand):**
   1. build (must succeed)
   2. format (must run before lint, since manual edits often need formatting that would otherwise show up as lint findings)
   3. in parallel: backend tests, restart Aspire, e2e tests (smoke for small changes; full suite for large changes; full suite at least every 5 commits)
   4. lint
   5. if lint or any earlier step changed anything, restart this validation from step 4.1

   **No conflicts (clean cherry-pick):**
   1. build (must succeed)
   2. in parallel: backend tests, format, lint, restart Aspire, e2e tests (same scope rules as above)
   3. if lint or any other parallel step changed anything, restart this validation from step 4.1

   E2E run with --stop-on-first-failure --quiet to fail fast. If tests fail, do not move on until the full suite is green. If something is missing because a later commit introduces it, pull only the minimum needed lines forward and note it under that future commit in the commits file as an indented line: '(partially pulled into earlier commit)'.

5. Fold all fixes into the cherry-picked commit — amend so the commit is self-contained: it builds, tests, lints, and passes e2e on its own. Re-run the sanitize step from #3 if the amend re-introduces any `# Conflicts:` block or `#`-prefixed lines.

6. Verify the working tree is clean — git status must show zero changes after the amend. If anything remains, you missed folding it in. Fix and amend again.

7. Mark the commit done — change [ ] to [x] on its line in the commits file.

8. Log learnings — append a one-line entry to {{learnings-file}} for any non-trivial adaptation, conflict resolution choice, or skipped piece. Format: '- <hash> — <one-line note>'. Skip routine cherry-picks that landed cleanly.

Mode-specific behavior:
- autonomous: do not ask the user anything. Make every judgement call yourself. Note unusual choices in {{learnings-file}}.
- interview-on-problem: when you cannot make progress (irreconcilable conflict, ambiguous intent, change that no longer makes sense in the new codebase), use AskUserQuestion to ask. Bake the answer into your next attempt and log it in {{learnings-file}}.

Operating principles:

- Never stop. If a commit cannot be made to work as-is, reshape the plan: collapse it with a neighbor, split it, reorder it, or drop changes that are no longer needed. The goal is a working rebuild, not literal preservation of every original commit.
- Smallest possible change when borrowing forward — pull only the lines required to compile or pass tests, never whole files or unrelated changes.
- Trust the loop. Every iteration ends with a clean tree, a passing build, passing tests, passing lint, passing e2e, and one more [x] in the commits file.
```
