---
name: format
description: Auto-format code via the developer CLI - backend (.NET via JetBrains cleanupcode), frontend (oxfmt + oxlint --fix), and the developer CLI itself.
---

# Format

```bash
dotnet run --project developer-cli -- format [--backend] [--frontend] [--cli] [--self-contained-system <name>] [--no-build] [--all-files] --quiet
```

Use `developer-cli` exactly as written - do not expand to an absolute worktree path.

- `--backend` - .NET (JetBrains cleanupcode)
- `--frontend` - React/TypeScript (oxfmt + oxlint --fix)
- `--cli` - the developer CLI itself
- `--self-contained-system <name>` - narrows backend formatting to one SCS (e.g. `account`, `main`)
- `--no-build` - skip the `dotnet tool restore` step (faster after a recent run)
- `--all-files` - format every file in the solution. Default is to format only `.cs` files changed against `origin/main` (faster).

No arguments formats everything (changed-only by default). Unformatted code fails CI - commit all changes, never revert.

After `build` succeeds, run `format`, `lint`, `test` in parallel with `--no-build`.

## Examples

```bash
dotnet run --project developer-cli -- format --quiet                                            # everything
dotnet run --project developer-cli -- format --backend --quiet                                  # all backend
dotnet run --project developer-cli -- format --frontend --quiet                                 # frontend
dotnet run --project developer-cli -- format --backend --self-contained-system account --quiet  # one SCS
```

## Always pass --quiet

Verbose output goes to a log file. On success the CLI prints a single line; on failure it prints a short error message - read the log if you need details. Backend is slow - run last. Frontend is fast.
