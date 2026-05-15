---
name: lint
description: Lint code via the developer CLI - backend (.NET via JetBrains inspectcode), frontend (oxlint), and the developer CLI itself.
---

# Lint

```bash
dotnet run --project developer-cli -- lint [--backend] [--frontend] [--cli] [--self-contained-system <name>] [--no-build] [--changed-only] --quiet
```

Use `developer-cli` exactly as written - do not expand to an absolute worktree path.

- `--backend` - .NET (JetBrains inspectcode)
- `--frontend` - React/TypeScript (oxlint)
- `--cli` - the developer CLI itself
- `--self-contained-system <name>` - narrows backend linting to one SCS (e.g. `account`, `main`)
- `--no-build` - skip the rebuild step (faster after a recent build)
- `--changed-only` - lint only `.cs` files changed against `origin/main` (much faster; see guidance below)

No arguments lints the whole solution. Every finding fails CI regardless of severity - fix all of them.

After `build` succeeds, run `format`, `lint`, `test` in parallel with `--no-build`. Backend lint is slow - run last. Frontend lint often needs code rewrites - run after each bigger change.

## When to use `--changed-only`

Inspectcode has cross-file rules ("unused public method", "member can be private", flow analysis across method calls). `--changed-only` only inspects the listed files - it doesn't catch issues in untouched files that became invalid because of edits elsewhere.

- **Routine work:** use `--changed-only`. Most lint findings are local (style, naming, hints) and the saving is large (~4m → ~30s).
- **Larger changes that affect other files** (refactoring a public API, deleting a method's only caller, changing a widely-used type): omit `--changed-only` and lint the full solution.

CI always lints the full solution, so anything missed by a local `--changed-only` run gets caught before merge.

## Examples

```bash
dotnet run --project developer-cli -- lint --quiet                                                # everything (full solution)
dotnet run --project developer-cli -- lint --backend --changed-only --quiet                       # backend, changed files only (recommended for routine work)
dotnet run --project developer-cli -- lint --frontend --quiet                                     # frontend
dotnet run --project developer-cli -- lint --backend --self-contained-system main --quiet         # one SCS, full
```

## Always pass --quiet

Verbose output goes to a log file. On success the CLI prints a single line; on failure it prints where to find the findings and exits 1.
