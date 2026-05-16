---
name: aspire-stop
description: Stop the .NET Aspire AppHost and its Docker containers via the developer CLI. Defaults to the current worktree; supports stopping all worktrees or a specific base port.
---

# Stop Aspire

```bash
dotnet run --project developer-cli -- stop [--all] [--port <basePort>]
```

Use `developer-cli` exactly as written - do not expand to an absolute worktree path.

Stops the Aspire AppHost **and** its associated Docker containers (postgres, azurite, mailpit, stripe-cli) - not just the Aspire process. Persistent containers survive a plain `aspire stop` by design, so this skill cleans them up too.

- (no flag) - stop the current worktree's stack
- `--all` - stop every worktree of this repository (via `git worktree list`). Only use when the user explicitly asks to stop everything - never as a default
- `--port <basePort>` - stop the worktree on that base port (e.g. `9000`, `9100`, `9200`); also cleans up Docker containers for that port even if the worktree was deleted

The flags are mutually exclusive.

## When to use

- To free up ports without restarting (rare - `aspire-restart` is the everyday default).
- To stop a stack from another worktree without switching to it (`--port`).
- Before deleting a worktree, to ensure its Docker containers don't leak.

## Output

The CLI prints what it stopped (Aspire process trees + each `docker rm --force`'d container) and exits.
