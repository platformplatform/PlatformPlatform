---
trigger: always_on
description: You have access to several tools and MCP servers
---

# MCP Servers

* BrowserMCP: Use this to troubleshoot frontend issues. The base URL is https://localhost:9000. The website is already running, so never start it manually.
* `[PRODUCT_MANAGEMENT_TOOL]`: Use this MCP tool to create and manage product backlog items. 

If an MCP Server is not responding, instruct the user to activate it rather than using workarounds like calling `curl` when the browser MCP is unavailable.

# Product Management Tools

> **Update this value in ONE place:**
>
> ```
> PRODUCT_MANAGEMENT_TOOL="Linear"
> ```
>
> Whenever you see `[PRODUCT_MANAGEMENT_TOOL]`, replace it with the value of the variable.

# Developer CLI Commands Reference

**For AI Agents**: Use the MCP tools directly via `mcp__platformplatform-developer-cli__<command>` (no alias needed).

**For Human Developers**: Use the `[CLI_ALIAS]` command-line tool. The `[CLI_ALIAS]` is installed in the path and can be run from any location with the same result.

Always use the Developer CLI (via MCP tools for AI, via CLI alias for humans) to build, test, and format code correctly over using direct commands like `npm run format` or `dotnet test`.

**IMPORTANT:** Never fall back to using direct commands like `npm run format`, `dotnet test`, `npx playwright test`, `npm test`, etc. Always use the proper method.

## CLI Alias Configuration (Human Developers Only)

> **Update this value in ONE place:**
>
> ```
> CLI_ALIAS="pp"
> ```
>
> Whenever you see `[CLI_ALIAS]` in human-facing documentation, replace it with the value of the `CLI_ALIAS` variable.

## Build Commands

Use these commands continuously when you are working on the codebase.

**AI Agents - Use MCP Tool**:
- `mcp__platformplatform-developer-cli__build(target=BuildTarget.Both, solutionName=null)`
  - Build both: `build()` or `build(BuildTarget.Both)`
  - Backend only: `build(BuildTarget.BackendOnly)`
  - Frontend only: `build(BuildTarget.FrontendOnly)`
  - Specific solution: `build(BuildTarget.BackendOnly, "AccountManagement.slnf")`

**Human Developers - Use CLI**:
```bash
# Build both backend and frontend
[CLI_ALIAS] build

# Build only backend
[CLI_ALIAS] build --backend

# Build specific backend solution
[CLI_ALIAS] build --backend --solution-name <solution-name>

# Build only frontend
[CLI_ALIAS] build --frontend
```

## Test Commands

After you have completed a backend task and want to ensure that it works as expected, run the test commands.

**AI Agents - Use MCP Tool**:
- `mcp__platformplatform-developer-cli__test(solutionName=null, noBuild=false)`
  - All tests: `test()`
  - Specific solution: `test(solutionName="AccountManagement.slnf")`
  - Skip build: `test(noBuild=true)`

**Human Developers - Use CLI**:
```bash
# Run all tests
[CLI_ALIAS] test

# Run tests for specific solution
[CLI_ALIAS] test --solution-name <solution-name>
```

## End-to-End Test Commands

**AI Agents - Use MCP Tool** (always runs with --quiet flag):
- `mcp__platformplatform-developer-cli__e2e(searchTerms=[], browser="all", ...)`
  - All tests: `e2e()`
  - Specific system: `e2e(selfContainedSystem="account-management")`
  - Specific browser: `e2e(browser="chromium")`
  - Search term: `e2e(searchTerms=["user management"])`
  - Smoke tests: `e2e(smoke=true)`
  - Note: MCP tool always runs with `--quiet` flag automatically

**Human Developers - Use CLI**:
```bash
# Run all end-to-end tests except slow tests
[CLI_ALIAS] e2e

# Run end-to-end tests for specific solution
[CLI_ALIAS] e2e --self-contained-system <self-contained-system-name>

# Run end-to-end tests for specific browser
[CLI_ALIAS] e2e --browser <browser-name>

# Run end-to-end tests for specific search term
[CLI_ALIAS] e2e <search-term>

# Run end-to-end tests for specific test tags
[CLI_ALIAS] e2e "@smoke"
[CLI_ALIAS] e2e "smoke"
[CLI_ALIAS] e2e "@comprehensive"
```

Any combination of the above parameters is possible. There are other parameters available, but you should only use the ones mentioned above.

## Format Commands

Run these commands before you commit your changes.

**AI Agents - Use MCP Tool**:
- `mcp__platformplatform-developer-cli__format(target=BuildTarget.Both, solutionName=null)`
  - Format both: `format()` or `format(BuildTarget.Both)`
  - Backend only: `format(BuildTarget.BackendOnly)`
  - Frontend only: `format(BuildTarget.FrontendOnly)`
  - Specific solution: `format(BuildTarget.BackendOnly, "AccountManagement.slnf")`

**Human Developers - Use CLI**:
```bash
# Format both backend and frontend (run this before commit)
[CLI_ALIAS] format

# Format only backend (run this before commit)
[CLI_ALIAS] format --backend

# Format specific backend solution (run this before commit)
[CLI_ALIAS] format --backend --solution-name <solution-name>

# Format only frontend (run this before commit)
[CLI_ALIAS] format --frontend
```

## Inspect Commands

Run code inspections to find quality issues.

**AI Agents - Use MCP Tool**:
- `mcp__platformplatform-developer-cli__inspect(target=BuildTarget.Both, solutionName=null, noBuild=false)`
  - Inspect both: `inspect()` or `inspect(BuildTarget.Both)`
  - Backend only: `inspect(BuildTarget.BackendOnly)`
  - Frontend only: `inspect(BuildTarget.FrontendOnly)`
  - Specific solution: `inspect(BuildTarget.BackendOnly, "AccountManagement.slnf")`

**Human Developers - Use CLI**:
```bash
# Inspect both backend and frontend
[CLI_ALIAS] inspect

# Inspect only backend
[CLI_ALIAS] inspect --backend --solution-name <solution-name>
```

## Check Commands

Run comprehensive checks (build, test, format, inspect).

**AI Agents - Use MCP Tool**:
- `mcp__platformplatform-developer-cli__check(target=BuildTarget.Both, solutionName=null, skipFormat=false, skipInspect=false)`
  - Check both: `check()` or `check(BuildTarget.Both)`
  - Backend only: `check(BuildTarget.BackendOnly)`
  - Frontend only: `check(BuildTarget.FrontendOnly)`
  - Skip format: `check(BuildTarget.BackendOnly, skipFormat=true)`

**Human Developers - Use CLI**:
```bash
# Check both backend and frontend
[CLI_ALIAS] check

# Check only backend
[CLI_ALIAS] check --backend

# Check only frontend
[CLI_ALIAS] check --frontend
```

## Watch Commands

Start .NET Aspire on https://localhost:9000 and force database migrations.

**AI Agents - Use MCP Tool** (always runs in detached mode with force restart):
- `mcp__platformplatform-developer-cli__watch()`
  - Start Aspire: `watch()`
  - Note: MCP tool always runs with `--detach --force` flags automatically

**Human Developers - Use CLI**:
```bash
# Start AppHost in detached mode
[CLI_ALIAS] watch --detach

# Force start (stop existing first)
[CLI_ALIAS] watch --detach --force

# With public URL
[CLI_ALIAS] watch --detach --public-url https://example.ngrok-free.app
```

## Command Breakdown

Using `--solution-name` with backend commands is recommended as it significantly reduces execution time compared to running commands against the entire codebase. Especially for the `format` and `inspect` commands.

**AI Agents**:
- `inspect(BuildTarget.BackendOnly, "BackOffice.slnf")`
- `format(BuildTarget.BackendOnly, "AccountManagement.slnf")`

**Human Developers**:
- `[CLI_ALIAS] inspect --backend --solution-name BackOffice.slnf`
- `[CLI_ALIAS] format --backend --solution-name AccountManagement.slnf`

The value of the `solutionName` parameter should be the solution filter file (`.slnf`) name from the self-contained system directory.

## Troubleshooting when `[CLI_ALIAS]` fails (Human Developers Only)

**AI Agents**: You don't need to troubleshoot the CLI - use MCP tools directly. If MCP tools fail, report the error to the user.

**Human Developers**: Only conduct these steps if the `[CLI_ALIAS]` command fails with a message like `command not found` or `/usr/bin/pp: No input files specified`.

1. Instruct the user to install the CLI by running `dotnet run install` from the `developer-cli` directory. NEVER install the CLI on behalf of the user.
2. The Developer CLI auto updates when there are changes to the codebase. If you see output like "The CLI was successfully updated. Please rerun the command." simply rerun the command.
3. If `CLI_ALIAS` at the beginning of this document doesn't match `<AssemblyName>` in `developer-cli/DeveloperCli.csproj`, instruct the user to update `CLI_ALIAS` to match `<AssemblyName>`.
4. macOS comes with a `pp` command that may conflict with the Developer CLI, given errors like `/usr/bin/pp: No input files specified`. Instruct the user to add this to the `~/.zshrc` file:

```bash
export PATH="/Users/[username]/.PlatformPlatform:$PATH"
```

Never start inspecting or fixing the Developer CLI. If it does not work, ask the user to fix it.

## ❌ Anti-patterns to avoid

**AI Agents**:
- Don't use Bash to call CLI commands - use MCP tools directly
- Don't try to troubleshoot CLI installation issues - report to user
- Never fall back to direct commands like `npm run format` or `dotnet test`
- Don't run format commands when you change code - only before commit

**Human Developers**:
- Don't change the working directory before running the CLI command
- Never fall back to using direct commands like `npm run format` or `dotnet test`
- Don't run `[CLI_ALIAS] format --frontend` when you change code, this is only needed before commit