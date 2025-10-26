---
trigger: glob
description: Azure DevOps-specific product management tool guide
globs: .claude/commands/process/**, .claude/commands/implement/**, .claude/commands/review/**, .claude/commands/orchestrate/**
---

# Azure DevOps

## Terminology Mapping

| Generic Term | Azure DevOps |
|---|---|
| `[Feature]` | Feature |
| `[Slice]` | User Story |
| `[Task]` | Task |
| `[Checklist]` | Checklist item |

## Status Mapping

| Generic Status | Azure DevOps |
|---|---|
| `[Planned]` | New |
| `[Active]` | Active |
| `[Review]` | Review |
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
| `featureId` | Feature ID |
| `sliceId` | User Story ID |
| `taskId` | Task ID |

## Critical Rules

- ALL operations MUST use MCP tools
- If ANY MCP call fails: STOP immediately and call report_problem with severity: error
- NEVER skip status updates
- NEVER work around MCP failures

## Initialization

Call any MCP command to check authentication. If not available or authentication fails: Stop workflow, tell user to check configuration.