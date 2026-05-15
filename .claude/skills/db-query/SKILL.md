---
name: db-query
description: Query the local Postgres database of the active Aspire worktree via psql.
---

# Database Query

**Port = `.workspace/port.txt` base + 2. Never trust Aspire MCP for the port — a common critical failure that silently runs SQL on another worktree's database.**

**Read-only. Every write needs explicit user approval for that exact statement, every time — prior approvals never carry over.**

**For destructive operations (DROP, TRUNCATE, DELETE, ALTER, or anything that loses data), take extreme care. If anything in the request is even slightly unclear about the scope, target, or intent, stop and ask for clarification before executing. Always assume the most conservative interpretation.**

## Prerequisites

If `psql --version` fails, tell the user:

> `psql` is not installed. Install:
> - **macOS**: `brew install libpq && brew link --force libpq`
> - **Linux**: `sudo apt install postgresql-client`
> - **Windows**: `winget install PostgreSQL.PostgreSQL.17`

## Connection

Aspire must be running.

- Host `localhost`, port = `<base> + 2` where `<base>` is the integer in `.workspace/port.txt`.
- User `postgres`.
- Password: `export PGPASSWORD=$(dotnet user-secrets list --project application/AppHost/AppHost.csproj | sed -n 's/^postgres-password = //p')` (keeps it out of shell history).

## Databases

Each SCS has its own database. List them:

```sql
SELECT datname FROM pg_database WHERE datistemplate = false AND datname <> 'postgres';
```
