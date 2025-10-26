---
trigger: glob
description: Linear-specific product management tool guide
globs: .claude/commands/process/**, .claude/commands/implement/**, .claude/commands/review/**, .claude/commands/orchestrate/**
---

# Linear

## Configuration

```
DEFAULT_TEAM=null
```

## Terminology Mapping

| Generic Term | Linear |
|---|---|
| `[Feature]` | Project |
| `[Story]` | Issue |
| `[Task]` | Sub-issue |
| `[Checklist]` | Checklist item |

## Status Mapping

| Generic Status | Linear |
|---|---|
| `[Planned]` | Todo |
| `[Active]` | In Progress |
| `[Review]` | In Review |
| `[Completed]` | Done |

## MCP Configuration

```json
{
  "mcpServers": {
    "linear-server": {
      "type": "http",
      "url": "https://mcp.linear.app/mcp"
    }
  }
}
```

## ID Mapping

| Generic ID | Linear |
|---|---|
| `featureId` | Project ID/name |
| `storyId` | Issue ID (e.g. PP-445) |
| `taskId` | Sub-issue ID (e.g. PP-484) |

## Critical Rules

- ALL operations MUST use MCP tools
- If ANY MCP call fails: STOP immediately and call report_problem with severity: error
- NEVER skip status updates
- NEVER work around MCP failures
- storyId will be an issue ID (e.g., "PP-445")
- taskId will be a sub-issue ID (e.g., "PP-484")

## Initialization

Call any MCP command to check authentication. If not available or authentication fails: Stop workflow, tell user to check configuration.
