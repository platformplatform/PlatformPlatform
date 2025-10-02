---
trigger: glob
description: Core rules for C# development and tooling
globs: *.cs,*.csproj,*.slnx,Directory.Packages.props,global.json,dotnet-tools.json
---

# Backend

Carefully follow these instructions for C# backend development, including code style, naming, exceptions, logging, and build/test/format workflow.

## Code Style

- Be consistent. If you do something a certain way, do all similar things in the same way.
- Always use these C# features:
  - Top-level namespaces.
  - Primary constructors.
  - Array initializers.
  - Pattern matching with `is null` and `is not null` instead of `== null` and `!= null`.
- Records for immutable types.
- Mark all C# types as sealed.
- Use `var` when possible.
- Use simple collection types like `UserId[]` instead of `List<UserId>` whenever possible.
- JetBrains tooling is used for automatically formatting code, but automatic line breaking has been disabled for more readable code:
  - Wrap lines if "new language" constructs are started after 120 characters. This allows lines longer than 120 characters but ensures that no "important code" is hidden after the 120 character mark.
- Avoid using exceptions for control flow:
  - When exceptions are thrown, always use meaningful exceptions following .NET conventions.
  - Use `UnreachableException` to signal unreachable code that cannot be reached by tests.
  - Exception messages should include a period.
- Log only meaningful events at appropriate severity levels.
  - Logging messages should not include a period.
  - Use structured logging.
- Never introduce new NuGet dependencies.
- Don't do defensive coding (e.g., do not add exception handling to handle situations we don't know will happen).
- Use `user?.IsActive == true` over `user != null && user.IsActive == true`.
- Avoid try-catch unless we cannot fix the reason. We have global exception handling to handle unknown exceptions.
- Use `SharedInfrastructureConfiguration.IsRunningInAzure` to determine if we are running in Azure.
- Use `TimeProvider.System.GetUtcNow()` and not `DateTime.UtcNow()`.
- Names rules:
  - Never use acronyms or abbreviations. E.g., use `SharedAccessSignature` instead of `Sas`, and `Context` over `Ctx`.
  - Prefer long variable names for better readability. E.g. `gravatarHttpClient` over `httpClient`, and `enterKeyListenerCancellationTokenSource` over `enterKeyListenerCancellation`.
  - Choose descriptive and unambiguous names.
  - Make meaningful distinction.
  - Use pronounceable names.
  - Use searchable names.
  - Replace magic numbers with named constants.
  - Avoid encodings. Don't append prefixes or type information.
- Comments rules:
  - Don't explain what you change (that belongs to commit messages). Code should reflect the current state AND never refer how it used to work, or what have been changed.
  - Always try to explain yourself in code.
  - Don't be redundant.
  - Don't add obvious noise.
  - Don't use closing brace comments.
  - Don't comment out code. Just remove.
  - Use as explanation of intent.
  - Use as clarification of code.
  - Use as warning of consequences.
- Source code structure:
  - Separate concepts vertically.
  - Related code should appear vertically dense.
  - Declare variables close to their usage.
  - Dependent functions should be close.
  - Similar functions should be close.
  - Place functions in the downward direction.
  - Public functions should be above internal functions, and internal functions should be above private functions.
  - Don't use horizontal alignment.
  - Use white space to associate related things and disassociate weakly related.
  - Avoid nesting of code. Prefer early return, or break/continue statements. Keep the happy path return at the end when possible.
- Functions rules :
  - Small.
  - Do one thing.
  - Use descriptive names.
  - Prefer fewer arguments.
  - Have no side effects.
  - Don't use flag arguments. Split method into several independent methods that can be called from the client without the flag.
- For enum comparisons:
  - When comparing enums to enums, use direct comparison: `tenant.State == tenantState.Trial`
  - When comparing string properties to enums (e.g., JWT claims, database string columns), use `nameof` on the enum: `executionContext.UserInfo.Role == nameof(UserRole.Owner)`.
  - Avoid unnecessary `Enum.TryParse` when the comparison context is clear and the string is expected to match the enum.

## Implementation

IMPORTANT: Always follow these steps very carefully when implementing changes:

1. Always start new changes by writing new test cases (or change existing tests). Remember to consult [API Tests](/.windsurf/rules/backend/api-tests.md) for details.
2. Build and test your changes:
   - Use the **build MCP tool** for backend. See [Tools](/.windsurf/rules/tools.md) for details.
   - Use the **test MCP tool** to run all tests.
   - If you change API contracts (endpoints, DTOs), use the **check MCP tool** for frontend to ensure it still compiles.
3. Format your code:
   - When all tests are passing and you think you are feature complete, use the **format MCP tool** for backend.
   - The format tool will automatically fix code style issues according to our conventions.

When you see paths like `/[scs-name]/Core/Features/[Feature]/Domain` in rules, replace `[scs-name]` with the specific self-contained system name (e.g., `account-management`, `back-office`) you're working with. Replace `[Feature]` with the specific feature name you're working with (e.g., `Users`, `Tenants`, `Authentication`). A feature is often 1:1 with a domain aggregate (e.g., `User`, `Tenant`, `Login`).