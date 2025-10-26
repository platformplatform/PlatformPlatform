---
trigger: glob
description: Product management tool configuration and terminology guide
globs: .claude/commands/process/**, .claude/commands/implement/**, .claude/commands/review/**, .claude/commands/orchestrate/**
---

# Product Management Guide

This guide defines product management tool configuration, terminology, and status mappings used across all workflows.

## Configuration

**Update these values in ONE place:**

```
PRODUCT_MANAGEMENT_TOOL="Linear"
DEFAULT_TEAM=null
```

Whenever you see `[PRODUCT_MANAGEMENT_TOOL]`, replace it with the value of the variable.

## Terminology Mapping

All workflows use generic terminology. A PRD describes a complete feature solution. Use this table to translate to tool-specific entities:

| Generic Term | Markdown | Linear | Azure DevOps | Jira |
|--------------|----------------|--------|--------------|------|
| `[feature]` | Feature markdown file | Project | Feature | Epic |
| `[slice]` | Slice markdown file | Issue | User Story | Story |
| `[task]` | Task header (## 1 Title) | Sub-issue | Task | Sub-task |
| `[checklist]` | Checkbox bullet (- [ ]) | Checklist item | Checklist item | Checklist item |

## Status Mapping

Generic status values map to tool-specific states:

| Generic Status | Markdown | Linear | Azure DevOps | Jira |
|----------------|----------|--------|--------------|------|
| Planned        | [Planned] | Todo | New | To Do |
| Active         | [Active] | In Progress | Active | In Progress |
| Review         | [Review] | In Review | Review | In Review |
| Completed      | [Completed] | Done | Closed | Done |

**Usage:**
- Engineers: Update task from "Planned" → "Active" on start, "Active" → "Review" before reviewer
- Reviewers: Update task to "Completed" if approved, back to "Active" if rejected
- Tech Lead: Update slice to "Review" when all tasks completed

## MCP Configuration

**Linear:**
```bash
claude mcp add --transport sse linear-server --scope [user|project] https://mcp.linear.app/sse
```

**Azure DevOps:**
Configure via `.mcp.json` (see Azure DevOps MCP documentation)

**Jira:**
Configure via `.mcp.json` (see Jira MCP documentation)

## Initialization

**If `[PRODUCT_MANAGEMENT_TOOL]` is "Markdown":**
- If `.workspace/task-manager` does not exist, run: `dotnet run --project developer-cli -- init-task-manager`

**If `[PRODUCT_MANAGEMENT_TOOL]` uses MCP:**
- Call any MCP command to check authentication
- If not available or authentication fails: Stop workflow, tell user to check configuration above

## File Structure

**For Markdown mode:**
```
.workspace/task-manager/
  └─ yyyy-MM-dd-[feature-title]/
      ├─ prd.md (feature description)
      ├─ 1-[slice-title].md (first slice with tasks)
      ├─ 2-[slice-title].md (second slice with tasks)
      └─ ...
```

**Task format in slice files:**
```markdown
# Slice Title

**Purpose:** What this slice delivers
**NOT included:** Out of scope items
**Dependencies:** Prerequisites
**IMPORTANT:** Scope warning

## 1 Task title [Planned]
- [ ] Checklist item 1
- [ ] Checklist item 2

## 2 Another task title [Planned]
- [ ] Checklist item
```
