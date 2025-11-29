---
trigger: glob
globs: .windsurf/workflows/process/**, .windsurf/workflows/implement/**, .windsurf/workflows/review/**, .windsurf/workflows/orchestrate/**
description: Markdown-specific product management tool guide
---

# Markdown

Markdown is a file-based product management tool that does NOT use MCP. Instead, [features] and [tasks] are stored as markdown files in `.workspace/task-manager/`, simulating the same structure as tools like Jira, Azure DevOps, and Linear using local files.

## Terminology Mapping

| Generic Term | Markdown |
|---|---|
| `[Feature]` | Feature markdown file (prd.md) |
| `[Task]` | Task section in feature file |
| `[Subtask]` | Bullet point in task description |

## Status Mapping

**For [Feature]:**
| Generic Status | Markdown |
|---|---|
| `[Planned]` | [Planned] |
| `[Active]` | [Active] |
| `[Resolved]` | [Resolved] |

**For [Task]:**
| Generic Status | Markdown |
|---|---|
| `[Planned]` | [Planned] |
| `[Active]` | [Active] |
| `[Review]` | [Review] |
| `[Completed]` | [Completed] |

## ID Mapping

| Generic ID | Markdown |
|---|---|
| `featureId` | File path to prd.md |
| `taskId` | Task number (1, 2, 3) |

## Initialization

If `.workspace/task-manager` does not exist, run: `dotnet run --project developer-cli -- init-task-manager`, which will create a nested .git repository, so these files are not tracked by the parent repository.

## File Structure

```
.workspace/task-manager/
  └─ yyyy-MM-dd-[feature-title]/
      └─ prd.md (feature description with tasks)
```

## Task Format in Feature Files

```markdown
# Feature Title

**Purpose:** What this feature delivers
**NOT included:** Out of scope items
**Dependencies:** Prerequisites

## 1 Task title [Planned]

Task description paragraph explaining what this task delivers.

- Subtask item 1
- Subtask item 2

## 2 Another task title [Planned]

Task description paragraph.

- Subtask item
```

## Task Identification

- [Task] identifiers are numeric (1, 2, 3, ...).
- [Task] numbers are sequential within each [feature] file.
- [Task] headers include numbers: `## 1 Task title [Status]`.

## Critical Rules

- featureId is a file path (e.g., ".workspace/task-manager/2025-11-29-feature-title/prd.md").
- taskId is a task number (e.g., "1", "2", "3").
- Update status by editing markdown file headers.
