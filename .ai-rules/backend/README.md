# Backend

When working with C# code, follow these rules very carefully.

## Code Style

- Always uses these C# 8 features:
  - Top-level namespaces. 
  - Primary constructors.
  - Array initializers.
  - Pattern matching with `is null` and `is not null` instead of `== null` and `!= null`.
- Records for immutable types.
- Mark all C# types as sealed.
- Use `var` when possible.
- Use simple collection types like `UserId[]` instead of `List<UserId>` when ever possible.
- JetBrain tooling is used for automatically formatting code, but automatic line breaking has been disabled for more readable code:
  - Wrap lines if "new language" constructs are started after 120 characters. This ensures that no important code is hidden after the 120 character mark.
- Use clear names instead of making comments.
- Never use acronyms.
- Avoid using exceptions for control flow:
  - When exceptions are thrown, always use meaningful exceptions following .NET conventions.
  - Use `UnreachableException` to signal unreachable code, that cannot be reached by tests.
  - Exception messages should include a period.
- Log only meaningful events at appropriate severity levels.
  -Logging messages should not include a period.
  - Use structured logging.
- Never introduce new NuGet dependencies.

## Implementation

IMPORTANT: Always follow these steps very carefully when implementing changes:

1. Consult any relevant rules files listed below and start by listing which rule files have been used to guide your response (e.g., `Rules consulted: commands.md, unit-and-integration-tests.md`).
2. Always start new changes by writing new test cases (or change existing tests). Remember to consult [Unit and Integration Tests](./unit-and-integration-tests.md) for details.
3. Always run `dotnet build` or `dotnet test` from `/application/` to verify the code compiles after each change.
4. Fix any compiler warnings or test failures before moving on to the next step.

When you see paths like `/[scs-name]/Core/Features/[Feature]/Domain` in rules, replace `[scs-name]` with the specific self-contained system name (e.g., `account-management`, `back-office`) you're working with. Replace `[Feature]` with the specific feature name you're working with (e.g., `Users`, `Tenants`, `Authentication`). A feature is often 1:1 with a domain aggregate (e.g., `User`, `Tenant`, `Login`).

## Backend Rules Files

- [API Endpoints](./api-endpoints.md) - Guidelines for minimal API endpoints.  
- [Commands](./commands.md) - Implementation of state-changing operations using CQRS commands.
- [Domain Modeling](./domain-modeling.md) - Implementation of DDD aggregates, entities, and value objects.
- [External Integrations](./external-integrations.md) - Implementation of integration to external services.
- [Queries](./queries.md) - Implementation of data retrieval operations using CQRS queries.
- [Repositories](./repositories.md) - Persistence abstractions for aggregates.
- [Strongly Typed IDs](./strongly-typed-ids.md) - Type-safe DDD identifiers for domain entities.
- [Telemetry Events](./telemetry-events.md) - Standardized observability event patterns.
- [Unit and Integration Tests](./unit-and-integration-tests.md) - Test suite patterns for commands, queries, and domain logic.

It is **EXTREMELY important that you follow the instructions in the rule files very carefully**.
