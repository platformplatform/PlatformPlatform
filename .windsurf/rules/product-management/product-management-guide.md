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

## ID Mapping

| Generic ID | Markdown | Linear | Azure DevOps | Jira |
|------------|----------|--------|--------------|------|
| featureId | File path to prd.md | Project ID/name | Feature ID | Epic ID/key |
| sliceId | File path to slice.md | Issue ID (e.g. PP-445) | User Story ID | Story ID/key |
| taskId | Task number (1, 2, 3) | Sub-issue ID | Task ID | Sub-task ID/key |

## Slice Ordering

**Critical:** When fetching slices from `[PRODUCT_MANAGEMENT_TOOL]`, always sort by manual order (sortOrder/position ascending). This respects the user's manual drag-and-drop ordering in the tool's UI. If slices appear in wrong order, the user can reorder them by dragging in `[PRODUCT_MANAGEMENT_TOOL]`, and the order will be consistent on the next read.

## Critical Rules for MCP Tools

**If `[PRODUCT_MANAGEMENT_TOOL]` uses MCP (Linear, Azure DevOps, Jira):**
- ALL operations MUST use MCP tools
- If ANY MCP call fails: STOP immediately and call report_problem with severity: error
- NEVER fall back to Markdown when `[PRODUCT_MANAGEMENT_TOOL]` is not available
- NEVER skip status updates
- NEVER work around MCP failures
- sliceId will be an issue ID (e.g., "PP-445")
- taskId will be a sub-issue ID (e.g., "PP-484")
- **Always fetch slices sorted by sortOrder/position ascending** (see Slice Ordering section above)

**If `[PRODUCT_MANAGEMENT_TOOL]` is "Markdown":**
- sliceId is a file path (e.g., ".workspace/task-manager/2025-10-25-feature/1-backend.md")
- taskId is a task number (e.g., "1", "2", "3")
- Update status by editing markdown file headers

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