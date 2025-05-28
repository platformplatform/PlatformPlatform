---
trigger: always_on
description: You have access to several tools and MCP servers
---


# MCP Servers

* BrowserMCP: Use this to troubleshoot frontend issues. The base URL is https://localhost:9000. The website is already running, so never start it manually.
* `[PRODUCT_MANAGEMENT_TOOL]`: Use this MCP tool to create and manage product backlog items. 

# Product Management Tools

> **Update this value in ONE place:**
>
> ```
> PRODUCT_MANAGEMENT_TOOL="Linear"
> ```
>
> Whenever you see `[PRODUCT_MANAGEMENT_TOOL]`, replace it with the value of the variable.

# Developer CLI Commands Reference

Use the `[CLI_ALIAS]` Developer CLI to build, test, and format backend and frontend code. The `[CLI_ALIAS]` is installed in the path and can be run from any location with the same result.

Always use the Developer CLI to build, test, and format code correctly over using direct commands like `npm run format` or `dotnet test`.

**IMPORTANT:** Never fall back to using direct commands like `npm run format` or `dotnet test`. Always use the Developer CLI with the appropriate alias.

## CLI Alias Configuration

> **Update this value in ONE place:**
> 
> ```
> CLI_ALIAS="pp"
> ```
> 
> Whenever you see `[CLI_ALIAS]`, replace it with the value of the `CLI_ALIAS` variable.

## Build Commands

Use these commands continously when you are working on the codebase.

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

```bash
# Run all tests
[CLI_ALIAS] test

# Run tests for specific solution
[CLI_ALIAS] test --solution-name <solution-name>
```

## Format Commands

Run these commands before you commit your changes.

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

## Command Breakdown

Using `--solution-name` with backend commands is recommended as it significantly reduces execution time compared to running commands against the entire codebase. Especially for the `format` and `inspect` commands.

- `[CLI_ALIAS] inspect --backend --solution-name BackOffice.slnf`
- `[CLI_ALIAS] format --backend --solution-name AccountManagement.slnf`

The value of the `--solution-name` parameter should be the solution filter file (`.slnf`) name from the self-contained system directory.

## Troubleshooting when `[CLI_ALIAS]` fails

Only conduct these steps if the `[CLI_ALIAS]` command fails with a message like `command not found` or `/usr/bin/pp: No input files specified`.

1. Instruct the user to install the CLI by running `dotnet run install` from the `developer-cli` directory. NEVER install the CLI on behalf of the user.
2. The Developer CLI auto updates when there are changes to the codebase. If you see output like "The CLI was successfully updated. Please rerun the command." simply rerun the command.
3. If `CLI_ALIAS` at the beginning of this document doesn't match `<AssemblyName>` in `developer-cli/DeveloperCli.csproj`, instruct the user to update `CLI_ALIAS` to match `<AssemblyName>`.
4. macOS comes with a `pp` command that may conflict with the Developer CLI, given errors like `/usr/bin/pp: No input files specified`. Instruct the user to add this to the `~/.zshrc` file:

```bash
export PATH="/Users/[username]/.PlatformPlatform:$PATH"
```

Never start inspecting or fixing the Developer CLI. If it does not work, ask the user to fix it.

## ❌ Anti-patterns to avoid
- Don't change the working directory before running the CLI command. The CLI can be run from any location with the same result.
- Never fall back to using direct commands like `npm run format` or `dotnet test`. Always use the Developer CLI with the appropriate alias.
- Don't run `[CLI_ALIAS] format --frontend` when you change code, this is only needed before commit.
