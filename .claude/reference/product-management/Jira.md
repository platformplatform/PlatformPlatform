# Jira

## Terminology Mapping

| Generic Term | Jira |
|---|---|
| `[Feature]` | Epic |
| `[Task]` | Story |
| `[Subtask]` | Bullet point in task description |

## Status Mapping

**For [Feature]:**
| Generic Status | Jira |
|---|---|
| `[Planned]` | To Do |
| `[Active]` | In Progress |
| `[Resolved]` | Done |

**For [Task]:**
| Generic Status | Jira |
|---|---|
| `[Planned]` | To Do |
| `[Active]` | In Progress |
| `[Review]` | In Review |
| `[Completed]` | Done |

## MCP Configuration

```json
{
  "mcpServers": {
    "atlassian": {
      "command": "npx",
      "args": ["-y", "mcp-remote", "https://mcp.atlassian.com/v1/sse"]
    }
  }
}
```

## ID Mapping

| Generic ID | Jira |
|---|---|
| `featureId` | Epic ID/key |
| `taskId` | Story ID/key |

## Critical Rules

- ALL operations MUST use MCP tools.
- If ANY MCP call fails: STOP immediately and call report_problem with severity: error.
- NEVER skip status updates.
- NEVER work around MCP failures.

## Initialization

Call any MCP command to check authentication. If not available or authentication fails: Stop workflow, tell user to check configuration.
