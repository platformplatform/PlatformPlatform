# Frontend

When working with TypeScript and React code, follow these rules very carefully.

## Code Style and Patterns

- Follow existing conventions for:
  - Naming (PascalCase for components, camelCase for variables/functions).
  - Structure (folder organization, file organization).
  - Always create semantically correct components with clear boundaries and responsibilities:
    - Each component should have a single, well-defined purpose.
    - UI elements with different functionality (e.g., navigation vs. data manipulation) should be in separate components.
    - Avoid mixing unrelated functionality in one component (e.g., don't add action buttons inside breadcrumb components).
- Biome is used for automatically formatting code.
- Avoid making changes to code that are not strictly necessary.
- Use clear names instead of making comments.
- Never use acronyms.
- Prioritize code readability and maintainability.
- Never introduce new npm dependencies.
- Use React Aria Components from `@repo/ui/components/ComponentName`.
- Use `onPress` instead of `onClick` for event handlers.
- Use `<Trans>...</Trans>` or t-string literals (t\`...\`) for translations (content should be plain English).
- Use TanStack Query for API interactions.

## Implementation

IMPORTANT: Always follow these steps very carefully when implementing changes:

1. Consult any relevant rules files listed below and start by listing which rule files have been used to guide your response (e.g., `Rules consulted: form-with-validation.md, tanstack-query-api-integration.md`).
2. Search the codebase for similar code before implementing new code.
3. Reference existing implementations to maintain consistency.
4. Always run `npm run build && npm run lint` (as a single command) from `/application/` to verify the code compiles.
5. Fix any compiler warnings or test failures.

## Frontend Rules Files

- [Form with Validation](./form-with-validation.md) - Forms with validation and API integration using TanStack Query.
- [Modal Dialog](./modal-dialog.md) - Modal dialog implementation patterns.
- [React Aria Components](./react-aria-components.md) - Usage of shared component library.
- [TanStack Query API Integration](./tanstack-query-api-integration.md) - Data fetching and mutation patterns.
- [Translations](./translations.md) - Internationalization implementation for UI text.

It is **EXTREMELY important that you follow the instructions in the rule files very carefully**.