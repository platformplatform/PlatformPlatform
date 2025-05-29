---
description: Workflow for creating and maintaining AI rules
---

# Update AI Rules Workflow

Follow this workflow to create and update AI rules files. Rules should be created in appropriate subfolders of the `.windsurf/` directory, with clear file names that reflect their purpose (e.g., `.windsurf/rules/backend/strongly-typed-ids.md`, `.windsurf/rules/frontend/tanstack-query-api-integration.md`).

## Workflow

1. Start each rule file with the required frontmatter:
   ```markdown
   ---
   description: Clear, one-line description of what the rule enforces.
   globs: path/to/files/*PostFix.ext, other/path/**/*
   alwaysApply: false
   ---
   ```
   - Make the description concise but descriptive.
   - Use appropriate glob patterns to target specific file types. Be as specific as possible, e.g. `**/Commands/*.cs` or `**/Domain/*Repository.cs`.
   - Set `alwaysApply` to false for most rules (except main.md which should have `alwaysApply: true`).

2. Follow this standard document structure for all rule files:
   - Start with a level 1 heading (`# Heading`) that matches the file name (without extension).
   - Begin with a brief overview paragraph describing the rule's purpose.
   - Include a level 2 heading called "## Implementation" that contains the implementation steps.
   - Include a level 2 heading called "## Examples" with practical examples.
   - Number implementation steps for easy reference.
   - Use bulleted lists for related items within a step.
   - Bullets should end with a period like a sentence.

3. Write implementation steps clearly and actionably:
   - Begin each step with a directive verb (Use, Follow, Create, Implement, etc.).
   - Be specific about requirements and conventions.
   - Provide clear guidance that can be followed without ambiguity.
   - Organize related requirements under the same numbered step using bullet points.
   - Cover both the "what" and the "how" in your instructions.

4. Reference related files and documentation:
   - Use `[filename](/path/to/file)` syntax to link to other files.
   - Example: `[Commands](/.windsurf/rules/backend/commands.md)` for rule references.
   - Example: `[UserRepository.cs](/application/account-management/Core/Features/Users/Domain/UserRepository.cs)` for code references.
   - Reference actual implementation examples from the codebase whenever possible.

5. Include clear code examples:
   - Use language-specific code blocks with proper syntax highlighting.
   - Always show both good (DO) and bad (DON'T) examples for clarity.
   - Use comments with ✅ DO: and ❌ DON'T: prefixes to highlight key points.
   - Structure examples in a consistent format:
   ```csharp
   // ✅ DO: Show good examples with clear explanation
   public class GoodExample
   {
       // Implementation details
   }

   // ❌ DON'T: Violate the convention
   public class BadExample
   {
       // Implementation details
   }
   ```
   - Provide multiple examples for complex rules, using numbered headings like "### Example 1" and "### Example 2".

6. Ensure consistency across all rule files:
   - Maintain consistent terminology across related rule files.
   - Use the same formatting conventions in all rules.
   - Ensure the level of detail is similar across rules of the same category.
   - Reference other rule files when dependencies exist between rules.
   - Keep file organization consistent within each directory.

7. Update rules when implementation patterns change:
   - Review and update rules when new patterns or libraries are introduced.
   - Ensure rules reflect the current best practices in the codebase.
   - Remove outdated guidance when development patterns evolve.
   - Add new examples that showcase modern implementations.

8. Organize rules logically by category:
   - Backend rules in `.windsurf/rules/backend/`
   - Frontend rules in `.windsurf/rules/frontend/`
   - Infrastructure rules in `.windsurf/rules/infrastructure/`
   - Developer CLI rules in `.windsurf/rules/developer-cli/`
   - Workflow instructions in `.windsurf/workflows/`

9. Remember that workflow files (in `.windsurf/workflows/`) are special:
   - These are not automatically included in AI context.
   - They are activated ad-hoc by the user when needed.
   - These should follow the same structure as other rule files.
   - They should have clear, actionable steps for completing specific workflows.

10. IMPORTANT: Never modify files in the `.windsurf/` directory:
    - Only modify files in the `.windsurf/` directory.
    - The `.windsurf/` directory is updated using the `[CLI_ALIAS] sync-windsurf-ai-rules` command.
    - Always run `[CLI_ALIAS] sync-windsurf-ai-rules` after updating rules and before committing.

11. Keep rule files under 6000 characters:
    - This limit is enforced by Windsurf.
    - Split complex rules into multiple files if necessary.

## Examples

### Example 1 - Rule file structure

```markdown
---
description: Guidelines for TypeScript code style, type safety, and best practices.
globs: **/*.ts,**/*.tsx
alwaysApply: false
---
# TypeScript

## Implementation

1. Use explicit type annotations for function parameters and return types.
2. Prefer interfaces over type aliases for object definitions.
3. Use proper error handling with typed catch clauses.
4. Initialize arrays and objects with concise syntax.
5. Use optional chaining and nullish coalescing for safer code.

## Examples

### Example 1 - Code Examples with DO and DON'T Patterns

```typescript
// ✅ DO: Use explicit return types
function calculateTotal(items: CartItem[]): number {
  return items.reduce((sum, item) => sum + item.price, 0);
}

// ❌ DON'T: Rely on type inference for function signatures
function calculateTotal(items) {
  return items.reduce((sum, item) => sum + item.price, 0);
}
```
```
