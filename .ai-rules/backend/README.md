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
  - Wrap lines if "new language" constructs are started after 120 characters. This allows lines longer than 120 characters, but ensures that no "important code" is hidden after the 120 character mark.
- Use clear names instead of making comments.
- Never use acronyms. E.g. use `SharedAccessSignature` instead of `Sas`.
- Avoid using exceptions for control flow:
  - When exceptions are thrown, always use meaningful exceptions following .NET conventions.
  - Use `UnreachableException` to signal unreachable code, that cannot be reached by tests.
  - Exception messages should include a period.
- Log only meaningful events at appropriate severity levels.
  - Logging messages should not include a period.
  - Use structured logging.
- Never introduce new NuGet dependencies.
- Don't do defensive coding (e.g. do not add exception handling to handle situations we don't know will happen).
- Avoid try-catch, unless we cannot fix the reason. We have Global Exception handling to handle unknown exceptions.
- Use SharedInfrastructureConfiguration.IsRunningInAzure to determine if we are running in Azure.
- Don't add comments unless the code is truly not expressing the intent.
- Never add XML Comments.
- Use DateTimeOffset and not DateTime
- Use TimeProvider.System.GetUtcNow() and not DateTime.UtcNow()

## Implementation

IMPORTANT: Always follow these steps very carefully when implementing changes:

1. Consult any relevant rules files listed below and start by listing which rule files have been used to guide your response (e.g., `Rules consulted: commands.md, unit-and-integration-tests.md`).
2. Always start new changes by writing new test cases (or change existing tests). Remember to consult [Unit and Integration Tests](unit-and-integration-tests.md) for details.
3. Build and test your changes:
   - Always run `dotnet run build --backend` from the [developer-cli](developer-cli) folder to build the backend.
   - Run `dotnet run test` from the [developer-cli](developer-cli) folder to run all tests.
   - During active development, you can skip running all tests until you're ready to verify everything.
4. Format your code:
   - When all tests are passing and you think you are feature complete, run `dotnet run format --backend` from the [developer-cli](developer-cli) folder.
   - The format command will automatically fix code style issues according to our conventions.

When you see paths like `/[scs-name]/Core/Features/[Feature]/Domain` in rules, replace `[scs-name]` with the specific self-contained system name (e.g., `account-management`, `back-office`) you're working with. Replace `[Feature]` with the specific feature name you're working with (e.g., `Users`, `Tenants`, `Authentication`). A feature is often 1:1 with a domain aggregate (e.g., `User`, `Tenant`, `Login`).

Please be aware that any change to the backend API requires a build to generate an OpenAPI contract, that will be used when building the frontend using `openapi-typescript` to generate the API contract used by the frontend.

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
