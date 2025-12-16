---
trigger: glob
globs: .claude/**
description: Guidelines for creating and updating AI rules and commands
---
# AI Rules

Guidelines for creating, updating, and reviewing AI configuration files (rules, commands/workflows, etc.). The `.claude/` directory is the source of truth, synchronized to other AI editors via the `sync_ai_rules` MCP tool.

## Directory Structure

- `.claude/rules/` - Coding standards and patterns in subfolders (e.g., backend, frontend, infrastructure)
- `.claude/commands/` - Slash command workflows in subfolders (e.g., process, modes, review)
- `.claude/reference/` - Reference documentation (not auto-loaded)

## Frontmatter Formats

### Rules Frontmatter

```yaml
---
paths: **/Commands/*.cs,**/Queries/*.cs
description: Clear, one-line description of what the rule enforces
---
```

- `paths:` - File patterns for conditional loading. Be specific (e.g., `**/Commands/*.cs` not `**/*.cs`)
- `description:` - Synced to other editors. Keep it self-contained

### Commands Frontmatter

```yaml
---
description: Self-contained description of what this workflow does
args:
  - name: argName
    description: What this argument is for
    required: false
---
```

- `description:` - Must be self-contained. Args are Claude Code specific and don't sync to other editors
- `args:` - Optional. Only available in Claude Code

## Writing Rules

1. Use the standard document structure:
   - Start with a level 1 heading (`# Title`) matching the filename (without extension)
   - Begin with a brief overview paragraph describing the rule's purpose
   - Include a level 2 heading `## Implementation` with numbered steps
   - Include a level 2 heading `## Examples` with practical examples

2. Follow formatting conventions:
   - Use Title Case for level 2 headings (e.g., `## Implementation`, `## Examples`)
   - Number implementation steps for easy reference
   - Use bulleted lists for related items within a step

3. Write implementation steps clearly:
   - Begin each step with a directive verb (Use, Follow, Create, Implement)
   - Be specific about requirements and conventions
   - Cover both the "what" and the "how" in your instructions

4. Include code examples:
   - Use language-specific code blocks with proper syntax highlighting
   - Show both good and bad examples for clarity
   - Use comments with `// ✅` and `// ❌` prefixes (optionally with `DO:` and `DON'T:`)
   - Provide multiple examples for complex rules, using `### Example 1` and `### Example 2`

5. Reference related files:
   - Use `[filename](/path/to/file)` syntax to link to other files
   - Reference actual implementation examples from the codebase whenever possible

## Writing Commands/Workflows

1. Use the standard document structure:
   - Start with `# Title Workflow` or `# Title Mode` heading
   - Use step-by-step structure for complex workflows (STEP 1, STEP 2, etc.)

2. Follow formatting conventions:
   - Use Title Case for level 2 headings
   - Descriptions must be self-contained since args don't sync to other editors

3. Reference other rules/commands with links when needed

## Product Management Tool Integration

Workflows that integrate with product management tools must use tool-agnostic terminology:

**✅ Use this terminology** (brackets, case, and pluralization consistently):
- `[feature]` / `[features]` / `[Feature]` / `[Features]` - A collection of related [tasks]
- `[task]` / `[tasks]` / `[Task]` / `[Tasks]` - A complete vertical slice of work
- `[subtask]` / `[subtasks]` / `[Subtask]` / `[Subtasks]` - Bullet points in [task] descriptions (not tracked separately)

**❌ Avoid these terms** (tool-specific):
- Issue, Epic, Story, User Story, Work Item, Ticket, Bug (as work item types)

The `[PRODUCT_MANAGEMENT_TOOL]` variable in `AGENTS.md` determines which specific tool guide to load. Reference tool-specific guides at `.claude/reference/product-management/[PRODUCT_MANAGEMENT_TOOL].md`.

## Sync and Limits

1. `.claude/` is the source of truth. Don't modify files in other editor directories directly

2. Run the `sync_ai_rules` MCP tool after updating files

3. **12,000 character limit** per file. Some editors like Windsurf truncate content exceeding this limit

## Review Checklist

When reviewing changes to rules or commands:

- [ ] Frontmatter format is correct for file type (rules vs commands)
- [ ] Description is self-contained and meaningful
- [ ] Level 2 headings use Title Case
- [ ] Examples use ✅/❌ patterns where applicable
- [ ] File organization matches its category
- [ ] Tool-agnostic terminology used (no Issue, Epic, Story, etc.)
- [ ] `sync_ai_rules` MCP tool will be run after changes

## Examples

### Example 1 - Rule File

```markdown
---
paths: application/*/Core/*/Commands/*.cs
description: Guidelines for implementing CQRS command handlers
---

# Commands

Command handlers implement write operations following CQRS patterns.

## Implementation

1. Create command record in the feature's Commands folder
2. Implement handler using MediatR IRequestHandler
3. Use FluentValidation for input validation

## Examples

### Example 1 - Command Records

` ` `csharp
// ✅ DO: Use records for commands
public sealed record CreateUserCommand(string Email, string Name) : IRequest<Result<UserId>>;

// ❌ DON'T: Use classes for commands
public class CreateUserCommand : IRequest<Result<UserId>>
{
    public string Email { get; set; }
    public string Name { get; set; }
}
` ` `
```

### Example 2 - Command File

```markdown
---
description: Workflow for implementing a [task] from a [feature]
args:
  - name: title
    description: Task title
    required: false
---

# Implement Task Workflow

You are implementing: **{{{title}}}**

## STEP 1: Read Task Assignment

Read the [task] details from [PRODUCT_MANAGEMENT_TOOL].

## STEP 2: Research Patterns

Find similar implementations in the codebase.

## STEP 3: Implement

Follow the relevant rules for your implementation area.
```
