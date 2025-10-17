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

# Developer MCP Tools

Use these MCP tools for development tasks. Never fall back to using direct commands like `npm run format`, `dotnet test`, `npx playwright test`, etc.

**IMPORTANT**: When you see "use the **build MCP tool**", call the MCP tool directly (not via Bash). MCP tools are available in your tool list.

## Build

Use the **build MCP tool** to compile code:
- Build both backend and frontend
- Build backend only
- Build frontend only
- Build specific self-contained system

## Test

Use the **test MCP tool** to run tests after completing backend tasks:
- Run all tests
- Run tests for specific self-contained system
- Skip build if code is already compiled

## E2E Tests

Use the **e2e MCP tool** to run end-to-end Playwright tests:
- Run all tests (automatically uses quiet mode)
- Filter by search terms, browser, or test tags
- Run smoke tests only
- See [E2E Tests](/.claude/rules/end-to-end-tests/e2e-tests.md) for patterns

## Format

Use the **format MCP tool** before committing code:
- Format both backend and frontend
- Format backend only
- Format frontend only
- Format specific self-contained system

## Inspect

Use the **inspect MCP tool** to run static code analysis:
- Inspect both backend and frontend
- Inspect backend only
- Inspect specific self-contained system
- Results saved to `/application/result.json`

## Comprehensive Validation Workflow

For comprehensive validation before committing:
1. First, run **build MCP tool** to compile code
2. Then run these MCP tools **in parallel** for maximum speed:
   - **test MCP tool** (with `--no-build`) - Run all tests
   - **format MCP tool** - Format code
   - **inspect MCP tool** (with `--no-build`) - Run static analysis

All tools support backend/frontend flags and specific self-contained systems.
All tools must pass with exit code 0 before committing.

## Watch

Use the **watch MCP tool** to start .NET Aspire at https://localhost:9000:
- Starts all services and containers
- Forces database migrations
- Runs automatically in detached mode

## Init Task Manager

Use the **init MCP tool** to initialize the task-manager directory:
- Creates task-manager as git submodule
- Automatically excluded from main repository

## Command Optimization

Using specific self-contained systems significantly reduces execution time:
- Faster: Check specific system (e.g., "account-management")
- Slower: Check entire codebase

The self-contained system name should match the folder name in `/application/`.

## Anti-patterns

- ❌ Don't use Bash to call commands directly
- ❌ Never fall back to `npm run format`, `dotnet test`, etc.
- ❌ Don't run format during development - only before commit
- ❌ Don't skip the check step before committing
- ✅ Always use MCP tools for all development tasks