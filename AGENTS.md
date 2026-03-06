## Build, Test, and Format

Always use MCP tools (`build`, `test`, `format`, `lint`, `run`, `end_to_end`) instead of running dotnet/npm/npx commands directly. Run `build` first, then remaining tools with `noBuild=true`.

**Slow:** Aspire restart, backend format, backend lint, end-to-end tests. **Fast:** frontend format/lint, backend test. If any slow operation is needed, run everything in parallel Task agents. End-to-end tests use `waitForAspire=true`.

**Aspire**: The `run` MCP tool starts the AppHost at [APP_URL]. Restart when backend changes or hot reload breaks. In the agentic workflow, only the Guardian agent calls the `run` MCP tool. All other agents must message the Guardian if they need Aspire restarted.

Never commit, amend, or revert without explicit user instruction each time. Commit messages: one descriptive line in imperative form, no description body. In the agentic workflow, only the Guardian agent commits. No other agent commits, stages, or unstages code.

## Application URL

Whenever you see `[APP_URL]`, replace it with the configured value.

```
APP_URL="https://localhost:9000"
```

## CLI Alias Configuration

Whenever you see `[CLI_ALIAS]`, replace it with the configured value.

```
CLI_ALIAS="pp"
```

## Product Management Tool

Whenever you see `[PRODUCT_MANAGEMENT_TOOL]`, replace it with the configured value.

```
PRODUCT_MANAGEMENT_TOOL="Linear"
```

## Project Structure

This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure.

- [application](/application): Contains application code, one folder per SCS, plus shared-kernel and shared-webapp.
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC).
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks.
