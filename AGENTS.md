## Behavioral Guidelines

1. **Think before coding.** State assumptions explicitly. If uncertain, ask rather than guess. When multiple interpretations exist, present them - don't pick silently.

2. **Goal-driven execution.** Define success criteria before iterating. Loop until verified. Strong success criteria let you loop independently; "make it work" requires constant clarification.

3. **Read before you write.** Before adding code in a file, read the file's exports, the immediate caller, and obvious shared utilities. "Looks orthogonal to me" is the most dangerous phrase in this codebase.

4. **Checkpoint significant steps.** After each step in a multi-step task, summarize what was done, what's verified, and what's left. Don't continue from a state you can't describe back.

5. **Fail loud.** Surface uncertainty, don't hide it. "Completed" is wrong if anything was skipped silently. "Tests pass" is wrong if any were skipped. "Feature works" is wrong if the edge case wasn't verified.

## Build, Test, and Format

Use the developer CLI skills (`build`, `test`, `format`, `lint`, `e2e`, `aspire-restart`, `team-interrupt`) for all code workflows. They invoke `dotnet run --project developer-cli -- <command>` directly. Never run `dotnet`, `npm`, or `npx` directly - the pre-tool-use Bash hook blocks them.

Run `build` first, then `format`, `lint`, `test` in parallel with `--no-build`.

**Slow:** Aspire restart, backend format, backend lint, end-to-end tests. **Fast:** frontend format/lint, backend test.

**Aspire**: The `aspire-restart` skill manages the AppHost - always use it; never `aspire run`, `aspire restart`, or the developer CLI's `run` command. Use the Aspire MCP `list_resources` tool to look up service URLs (or read `.workspace/port.txt` if you only need the base port). In the agentic workflow, only the Guardian agent restarts Aspire. All other agents must notify the Guardian if they need it restarted.

Never commit, amend, or revert without explicit user instruction each time. Commit messages: one descriptive line in imperative form, no description body.

## Product Management Tool

Whenever you see `[PRODUCT_MANAGEMENT_TOOL]`, replace it with the configured value.

```
PRODUCT_MANAGEMENT_TOOL="Linear"
```

When working with [features] or [tasks], read `.claude/reference/product-management/[PRODUCT_MANAGEMENT_TOOL].md` to learn how to look them up, how to update status, and how generic statuses like [Active], [Review], [Completed] map to the tool. Read the [feature] for full context and the [task] for specific requirements.

## Auto Memory

Never write to or edit any auto memory files (MEMORY.md or any file in a memory directory). These files are managed by the user only.

## Source of Truth

Always verify paths, names, and API routes against the actual codebase. Never rely on memory, cached context, or prior session knowledge for these. Always look them up. Only read files within the git repository unless explicitly asked to look elsewhere.

## Project Structure

This is a mono repository with multiple self-contained systems (SCS), each being a small monolith. All SCSs follow the same structure.

- [application](/application): Contains application code, one folder per SCS, plus shared-kernel and shared-webapp.
- [cloud-infrastructure](/cloud-infrastructure): Bash and Azure Bicep scripts (IaC).
- [developer-cli](/developer-cli): A .NET CLI tool for automating common developer tasks.
