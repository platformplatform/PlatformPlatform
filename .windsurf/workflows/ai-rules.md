---
description: Workflow for creating and maintaining AI rules
---

# AI Rules Workflow

Follow these steps to create and update AI rules files should be created in appropriate subfolders of the `.cursor/` directory, with clear file names that reflect their purpose (e.g., `.cursor/backend/strongly-typed-ids.mdc`, `.cursor/frontend/tanstack-query-api-integration.mdc`).

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
   - Set `alwaysApply` to false.

2. Always follow this standard document structure:
   - Start with a level 1 heading (`# Heading`) that matches the file name (without extension).
   - Follow with a level 2 heading called "## Implementation" that contains the implementation steps.
   - Include a level 2 heading called "## Examples" with practical examples.
   - Only deviate from this structure when absolutely necessary.
   - Number implementation steps for easy reference.
   - Use bulleted lists for related items and examples.
   - Bullets should end with a period like a sentence.

3. Reference related files and documentation:
   - Use `[filename](mdc:path/to/file)` syntax to link to other files.
   - Example: `[Commands](mdc:.cursor/rules/backend/commands.mdc)` for rule references.
   - Example: `[UserRepository.cs](mdc:application/account-management/Core/Features/Users/Domain/UserRepository.cs)` for code references.

4. Include clear code examples:
   - Use language-specific code blocks.
   - Show both good and bad examples for clarity.
   - Include comments to highlight key points.
   - Prefer multiple numbered examples (e.g., "Example 1", "Example 2").
   - Reference specific files using proper MDC links for examples:
     ```markdown
     # Example 1 - Simple example with only basic operations
     See [repositories.mdc](mdc:.cursor/rules/backend/repositories.mdc) for a complete example.
     
     # Example 2 - Complex example with search and pagination 
     See [queries.mdc](mdc:.cursor/rules/backend/queries.mdc) for examples with pagination.
     ```
   - For standard examples, use this format:
   ```typescript
   // ✅ DO: Show good examples
   const goodExample = true;
   
   // ❌ DON'T: Show anti-patterns
   const badExample = false;
   ```
   - See [Queries](mdc:.cursor/rules/backend/queries.mdc) for a reference of well-structured examples.

5. Structure rule content effectively:
   - Start with a high-level overview of the rule's purpose.
   - Include specific, actionable requirements.
   - Show examples of correct implementation.
   - Reference existing code when possible.
   - Keep rules DRY by referencing other rules.

6. Format content consistently:
   - Use bullet points for clarity and readability.
   - Keep descriptions concise but complete.
   - Include both DO and DON'T examples.
   - Reference actual code over theoretical examples.
   - Use consistent formatting across all rule files.

## Examples

### Example 1 - Rule File Structure

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
