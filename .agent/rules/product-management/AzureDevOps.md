---
trigger: glob
globs: .agent/workflows/process/**, .agent/workflows/implement/**, .agent/workflows/review/**, .agent/workflows/orchestrate/**
description: Azure DevOps-specific product management tool guide
---
# Azure DevOps

## Terminology Mapping

| Generic Term | Azure DevOps |
|---|---|
| `[Feature]` | User Story |
| `[Task]` | Task |
| `[Subtask]` | Bullet point in task description |

## Status Mapping

**For [Feature]:**
| Generic Status | Azure DevOps |
|---|---|
| `[Planned]` | New |
| `[Active]` | Active |
| `[Resolved]` | Resolved |

**For [Task]:**
| Generic Status | Azure DevOps |
|---|---|
| `[Planned]` | New |
| `[Active]` | Active |
| `[Review]` | Resolved |
| `[Completed]` | Closed |

## MCP Configuration

```json
{
  "mcpServers": {
    "azure-devops": {
      "command": "node",
      "args": ["path/to/mcp-server.js"],
      "env": {
        "AZURE_DEVOPS_ORG_URL": "https://dev.azure.com/YOUR_ORG",
        "AZURE_DEVOPS_PAT": "YOUR_PERSONAL_ACCESS_TOKEN"
      }
    }
  }
}
```

## ID Mapping

| Generic ID | Azure DevOps |
|---|---|
| `featureId` | User Story ID |
| `taskId` | Task ID |

## Critical Rules

- Descriptions support HTML formatting: use `<pre>` for ASCII art/code blocks, `<br/>` for line breaks, `<strong>` for bold.
- ALL operations MUST use MCP tools.
- If ANY MCP call fails: STOP immediately and call report_problem with severity: error.
- NEVER skip status updates.
- NEVER work around MCP failures.

## Initialization

Call any MCP command to check authentication. If not available or authentication fails: Stop workflow, tell user to check configuration.
