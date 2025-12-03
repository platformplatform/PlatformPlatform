---
trigger: glob
globs: .windsurf/workflows/process/**, .windsurf/workflows/implement/**, .windsurf/workflows/review/**, .windsurf/workflows/orchestrate/**
description: Linear-specific product management tool guide
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
| `[Task]` | Issue |
| `[Subtask]` | Bullet point in task description |

## Status Mapping

**For [Feature]:**
| Generic Status | Linear |
|---|---|
| `[Planned]` | Next |
| `[Active]` | Now |
| `[Resolved]` | Now |

**For [Task]:**
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
| `taskId` | Issue ID (e.g. PP-445) |

## Rules

- All operations use MCP tools.
- If an MCP call fails: Stop immediately and call report_problem with severity: error.
- Don't skip status updates.
- Don't work around MCP failures.
- featureId will be a project ID/name.
- taskId will be an issue ID (e.g., "PP-445").
- To query tasks in a feature: use `list_issues` with `project: "<project-id>"`.

## Initialization

Call any MCP command to check authentication. If not available or authentication fails: Stop workflow, tell user to check configuration.
