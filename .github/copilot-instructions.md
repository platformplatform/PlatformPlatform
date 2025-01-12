When replying, list *ALL* the sections from this guide that could be relevant to guide your response, and ensure to follow the guidance.For example:

Sections used:
- General Practices
- Backend Guidelines > Over all
- Backend Guidelines > API

# General Practices (always include this section)
- VERY IMPORTANT:
  - Avoid making changes to code that is not relevant (e.g., don’t remove comments or alter types).
  - Consistency is extremely important. Always look for similar code in the existing code base before adding new code, and do you utmost to follow conventions for naming, structure, patterns, formatting, styling, etc.
  - Ask questions if guidance is unclear instead of making assumptions.
- General info:
  - SCS means self-contained system.
  - Use long descriptive variable names (e.g commitMessage not commit).
  - Never use acronyms (e.g. SharedAccessSignatureLink not SasLink).

# Project Overview
PlatformPlatform is a multi-tenant SaaS foundation monorepo with:
- `application/`: Self-contained systems (SCSs) with WebApp, Api, Core, and Workers
- `cloud-infrastructure/`: Azure infrastructure (Bicep)
- `development-cli/`: Developer tools

# Backend Guidelines

## Over all
+ Always use new C# 8/9 language features like top-level namespaces, primary constructors, array initializers, and `is null`/`is not null` over `== null`/`!= null`.
- Only throw exceptions for exceptional cases.
- Prefer long lines, and break at 120-140 characters.
- Use TimeProvider.System.GetUtcNow() to get current time.
- All IDs on domain entities, commands, queries, API endpoints, etc. are strongly typed IDs. E.g. `TenantId Id` instead of `long Id`.
- IMPORTANT: After making backend changes, run `dotnet build` from `/application/` and `dotnet test --no-restore --no-build` to validate changes.

## API
- Implement in Endpoint namespace in the API project for the SCS.
- Always return Response DTOs.
- Always use strongly TypedIDs in contract.
- Implement Minimal API endpoints in a single line, calling `mediator.Send()` and convert any path parameters using the ` with { Id = id }` like shown here:

  ```csharp
  group.MapPut("/{id}", async Task<ApiResult> (TenantId id, UpdateTenantCommand command, IMediator mediator)
    => await mediator.Send(command with { Id = id })
  );

  [PublicAPI]
  public sealed record UpdateTenantCommand : ICommand, IRequest<Result>
  {
      [JsonIgnore] // Removes this property from the API contract
      public TenantId Id { get; init; } = null!;

      public required string Name { get; init; }
  }
  ```

## Command and Queries
- Follow vertical slice architecture: one file per feature under `/Features/[Feature]` in `[SCS].Core`:
  ```bash
  ├─ Features    # Typically the plural name of the aggregate
  │  ├─ Commands # One file for each command that includes the Command, CommandHandler, and Validator
  │  ├─ Domain   # One file for the Aggregate, Repository, AggregateEvents, AggregateTypes etc.
  │  ├─ Queries  # One file for each query that includes the Query, QueryHandler
  │  ├─ Shared   # Used for shared logic to avoid code duplication in commands
  ```
- Use `Result<T>/Result` from SharedKernel for return types, not `ApiResult<T>`.
- Apply `[PublicAPI]` from `JetBrains.Annotations` for all Command/Query/Response DTOs.
- MediatR pipelines validation behaviors are used to run input validation, handle domain events, commit unit of work, and send tracked events.
- When creating new commands always also create a new matching Telemetry event in `/Features/[TelemetryEvents]/` with meaningful properties, like here:
  ```csharp
    # Handler class
    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        ...
        loginRepository.Update(login);

        var loginTimeInSeconds = (int)(TimeProvider.System.GetUtcNow() - login.CreatedAt).TotalSeconds;

        events.CollectEvent(new LoginCompleted(user.Id, loginTimeInSeconds)); // Track just before returning

        return Result.Success();
    }

    # TelemetryEvents.cs
    public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));
  ```

## Repositories
- Should return domain objects or primary types (never PublicAPI DTOs).
- Repositories inherit from `RepositoryBase`, which has generic CRUD methods. E.g.to create a read-only repository, define a new interface with only read methods, utilizing only those from the `RepositoryBase`.
```csharp
  public interface ITenantRepository : IReadonlyRepository<Tenant, TenantId>
  {
      Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken);
  }

  internal sealed class TenantRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Tenant, TenantId>(accountManagementDbContext), ITenantRepository
  {
  }
  ```

## Aggregates, Entities, and Value Objects
- Inherit from `AggregateRoot`.
- Avoid aggregate references. Use queries instead of properties like `Login.User` to prevent inefficient joins.
- Use classes for Entities and records for Value Objects. E.g. `User.Avatar`,  `Order.OrderLines`.

## Integrations
- Create external service clients in `/Client/[ServiceName]/[ServiceClient].cs`.

## Testing
- Use XUnit and Fluent Assertions.
- Name in the form: `[Method_WhenX_ShouldY]`.
- Prefer testing API endpoints over writing unit tests.
- Use NSubstitute for mocks, but only mock integrations. Don't mock repositories.

# Frontend Guidelines

## Over all
- Emphasize accessibility and type safety.
- Leverage Tailwind variants for styling.
- Global, reusable components live in Shared-Web. Change here only if it’s universally needed.
- IMPORTANT: After making frontend changes, run `npm run build` from `/application`, and if successful run `npm run format` and `npm run check`.

## React Aria Components
- Build UI using components from `@application/shared-webapp/ui/components`.
- Use `onPress` instead of `onClick`.

## API integration
- A strongly typed API Contract is generated by the .NET API in each SCS (look for @/WebApp/shared/lib/api/api.generated.d.ts).
- When making API calls, don't use standard fetch, but instead use `@application/shared-webapp/infrastructure/api/PlatformApiClient.ts`, that contains methods for get, post, put, delete, options, head, patch, trace.

Here is an example of how to use the API client for a GET request:
  ```typescript
  await api.get("/api/account-management/users/{id}", {
    params: { path: { id: userId } }
  });
  ```

- Here is an example of how to use the API client for a POST request that is submitting a forms, use React Aria's Form component with `validationBehavior="aria"` for accessible form validation and error handling:
  ```typescript
  <Form action={api.actionPost("/api/account-management/signups/start")} validationErrors={errors} validationBehavior="aria">
    <TextField name="email" type="email" label="Email" isRequired />
    ...
    <Button type="submit">Submit</Button>
  </Form>
  ```
- Keep components in feature folders; focus on small, composable units.

## Dependencies
- Avoid adding new dependencies to the root package.json.
- If needed always Pin versions (no ^ or ~).
- Use React Aria Components before adding anything new.

# Git
- Use one-line imperative, sentence-case commit messages with no trailing dot; don't prefix with "feat," "fix," etc.
