---
name: upgrade-packages
description: Upgrade all backend (.NET/NuGet), frontend (npm), and GitHub Actions dependencies to their latest versions, in that fixed order. Drives the developer CLI's update-packages command, parses its quiet dry-run output to separate trivial bumps from majors, ships trivial bumps as one bulk commit per side, and gives every major (and any code/config change) its own clean commit. Detects required toolchain installs (e.g. a new .NET SDK that needs sudo) and asks the user to run them up front so the backend upgrades first. Fixes the CLI itself when it produces a wrong outcome.
---

# Upgrade Packages

Bring all backend, frontend, and GitHub Actions dependencies to their latest versions. The project always runs the latest version of every package. **Don't quit, give up, or recommend reverting just because something is non-trivial** — research, debug, push through. The only acceptable reasons to skip an upgrade are the permanent exceptions below.

## How To Drive The CLI

Every NuGet and npm bump goes through the developer CLI. Run it directly (the Bash hook allows `dotnet run --project developer-cli`, and the CLI runs its own `dotnet`/`npm` subprocesses without tripping the hook):

```bash
dotnet run --project developer-cli -- update-packages [--backend|--frontend] [--dry-run] [--exclude <csv>] [--include-major-framework-updates] --quiet
```

Always pass `--quiet`. In quiet mode the command prints no tables or banners — only parseable plain-text lines:

```
<side> <patch|minor|major> <package> <current> -> <new> [<extra>]
<side> restricted <package> <current> (latest <X> is a new major, pinned)
<side> excluded <package> <current>
summary <side> patch=<n> minor=<n> major=<n> excluded=<n> uptodate=<n>
```

`<side>` is `backend` or `frontend`. `restricted` lines are the permanent exceptions (pinned to their current major by the CLI). Use a `--dry-run --quiet` run to plan; parse the `major` lines to get the package names that need their own commit.

Run the **build**, **format**, **lint**, **test**, and **e2e** skills for all verification (never raw `dotnet`/`npm`).

## Permanent Exceptions

These never move to a new major. **The CLI enforces them itself** (`RestrictedNuGetPackages` in `developer-cli/Commands/UpdatePackagesCommand.cs`), so you do **not** pass `--exclude` for them — they show up as `restricted` lines in the dry-run and are pinned to the latest version within their current major:

- **`MediatR`**, **`FluentAssertions`** — later majors changed licensing/APIs.
- **`Microsoft.ApplicationInsights`**, **`Microsoft.ApplicationInsights.AspNetCore`** — the next major drops `PageView` tracking as part of moving to OpenTelemetry; the codebase uses `PageView` heavily and that migration is a separate effort.

Note: frontend `@microsoft/applicationinsights-*` packages are **not** restricted and upgrade normally. `.NET`, `Node.js`, and `@types/node` stay within their current major unless you pass `--include-major-framework-updates`; don't cross a framework major as part of a routine package upgrade.

## Principles

1. **Use `update-packages` for every bump.** Never hand-edit `package.json` / `Directory.Packages.props` or run raw `npm` / `dotnet` to move a version.
2. **The CLI must produce a correct outcome. If it doesn't, fix the CLI** in `developer-cli/Commands/UpdatePackagesCommand.cs`, and commit that fix on its own. Working around a CLI bug by hand is unacceptable — the next person deserves the fix.
3. **Order is fixed and never reordered: backend (.NET) first, then frontend, then GitHub Actions last.** If the backend is blocked because a new toolchain (a .NET SDK) isn't installed, that install needs sudo/admin — ask the user to run it up front (Workflow step 3) and wait. Never skip ahead to the frontend to "stay busy" while a backend toolchain install is pending.
4. **Atomic commits.** Trivial bumps (patch + minor) go into one bulk commit per side. Every major — and anything needing a code change, config change, or API rename — gets its own commit with the change.
5. **Research majors online.** Read the changelog, release notes, and GitHub issues for every major before applying it. If a new major exposes cheap, obvious improvements, adopt them in the same commit. If adoption is non-trivial, ship the upgrade and note the follow-up.
6. **Verify smartly, and gate each phase.** `build` + `lint` per trivial commit; add `e2e` after majors that touch runtime, build tooling, or i18n; run `format` whenever code changes or after upgrading formatter/linter tooling. Run a **full backend regression with `e2e` at the end of the backend phase, before the frontend**, and a full regression for both sides at the very end. Fold any fix into the commit that caused it (see **Fixing A Regression**).
7. **Push through.** When an upgrade misbehaves, figure out *why*. Reverting is the last resort, only after evidence the version is genuinely unusable.

## Fixing A Regression

When a regression run fails, the bad change belongs in an earlier commit — fold the fix there, don't tack a loose fix on the end. The branch isn't pushed yet, so rewriting local history is safe. Two patterns by cause:

- **The upgrade is fine but code must adapt** — make the code/config change, then fold it into the commit that introduced the breakage: `git commit --fixup=<offending-commit>` followed by `git rebase --autosquash --interactive <base>`.
- **One package in a bulk commit is the culprit** — pull just that package out of the bulk so the bulk stays green (re-run the bulk with that package added to `--exclude`, or drop its version bump from the bulk commit via a fixup), then give it its own commit on top with the code change it needs — exactly like a major.

Either way, keep every commit independently green and bisectable, and never move to the next phase with a red suite — the fix-up happens in the phase that caused it.

## Workflow

1. **Verify clean baseline** — `git status` clean; `build`, `lint`, `test` green (run `e2e` if anything looks risky). Fix or stop if the baseline is broken before you start.

2. **Dry-run** — `update-packages --dry-run --quiet` (no side flag covers both). Read the output and split each side into:
   - **Trivial** = every `patch` and `minor` line.
   - **Majors** = every `major` line. Collect the package names; each becomes its own commit.
   - **Toolchain** = any `(sdk)` line (e.g. `dotnet-sdk`) and any `⚠️ … is NOT installed` warning — these gate the backend and are handled first (step 3).
   - `restricted` lines are the permanent exceptions — ignore them.

3. **Toolchain prerequisites (sudo) — resolve before any upgrade** — if the dry-run reports a `dotnet-sdk` (or other framework) bump whose target version is not installed locally, the backend update will abort: `update-packages --backend` exits when the required SDK is missing. You **cannot** install it yourself — it needs sudo/admin. **Stop and ask the user to run the exact install command the dry-run printed** (e.g. `brew upgrade dotnet-sdk` on macOS, `winget upgrade Microsoft.DotNet.SDK.<major>` on Windows), then wait for them to confirm. Re-run `update-packages --dry-run --quiet` and check that nothing is still flagged as not-installed before continuing. This keeps the backend first and unblocked — do not start the frontend while a backend toolchain install is pending.

4. **Backend bulk (trivial)** — apply all backend patch/minor at once, excluding the majors so only safe bumps land:
   ```bash
   dotnet run --project developer-cli -- update-packages --backend --exclude <comma-separated backend majors> --quiet
   ```
   Then `build --backend` + `lint --backend`. When green, commit, e.g. `Upgrade backend NuGet packages, dotnet tools, and SDK to latest minor and patch versions`.

5. **Backend majors, one at a time** — for each backend major package `P` (the only outstanding backend updates after the bulk are the majors, so exclude the *other* majors to move just `P`):
   ```bash
   dotnet run --project developer-cli -- update-packages --backend --exclude <every other backend major> --quiet
   ```
   Research `P`'s changelog, make the required code changes, adopt cheap new features, run `build`/`format`/`lint`/`test` (+ `e2e` if it touches runtime), and commit `P` and its changes together, e.g. `Upgrade <Package> to <version> and <what changed>`.

6. **Backend regression gate — full suite with `e2e`, before the frontend** — with every backend commit in place, run the full backend suite: `build --backend`, `format --backend`, `lint --backend`, `test`, and `e2e`. The backend must be fully green here, before any frontend work begins — that way a failure is unambiguously a backend regression and its fix lands in a backend commit. If it fails, fix it up now (see **Fixing A Regression**); never carry a red backend suite into the frontend phase.

7. **Frontend bulk (trivial)** — same as step 4 with `--frontend`. The CLI runs `npm install` and `npm audit fix` for you. Then `build --frontend` + `lint --frontend` (+ `format --frontend` since the install may reformat). Commit, e.g. `Upgrade frontend npm dependencies to latest minor and patch versions`.

8. **Frontend majors, one at a time** — same as step 5 with `--frontend`. Formatter/linter majors (oxfmt, oxlint) and i18n/build-tool majors warrant a `format` + `e2e` pass. One commit per major.

9. **GitHub Actions — the last upgrade** — only after backend and frontend are fully done, bump the workflow dependencies in `.github/workflows/*.yml` (not covered by `update-packages`):
   - Each `uses: <action>@vN` to its latest major (e.g. `actions/checkout`, `actions/setup-node`, `actions/setup-dotnet`, `actions/setup-java`, `actions/upload-artifact`, `actions/download-artifact`, `actions/github-script`, `azure/login`, `docker/setup-buildx-action`).
   - `runs-on:` runners to the current Ubuntu LTS image (e.g. `ubuntu-24.04`).
   - Pinned tool versions inside `with:` (`node-version`, and any others) to match the project's runtime.
   Verify nothing else references an old version; commit, e.g. `Upgrade GitHub Actions and runner images to latest versions`.

10. **Final regression** — close by running the full set for both sides: `build`, `format`, `lint`, `test`, `e2e`. If anything fails, fix it up in the commit that caused it (see **Fixing A Regression**) rather than tacking a fix on the end. Then summarise to the user: what moved, what was skipped (with reason), what was adopted, what's deferred.

## Success

Every non-exception package on its latest version. Trivial bumps in one bulk commit per side; each major and each code/config change in its own clear commit; GitHub Actions current. The CLI is better than when you started — every bug you tripped over is fixed at the source and committed separately. `build`, `format`, `lint`, `test`, and `e2e` all green at HEAD.
