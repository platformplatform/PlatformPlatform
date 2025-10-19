---
trigger: glob
description: Rules for CQRS queries, including structure, validation, response types, and mapping
globs: **/Queries/*.cs
---

# CQRS Queries

Carefully follow these instructions when implementing CQRS queries, including structure, validation, response types, and MediatR pipeline behaviors.

## Implementation

1. Create queries in the `/[scs-name]/Core/Features/[Feature]/Queries` directory.
2. Create one file per query containing Query, Response, Validator (optional), and Handler:
   - Name the file after the query without suffix: e.g., `GetUsers.cs`.
3. Query Record:
   - Create a public sealed record marked with `[PublicAPI]` that implements `IRequest<Result<TResponse>>`.
   - Name with `Query` suffix: e.g., `GetUsersQuery`.
   - Define properties in the primary constructor.
   - Use property initializers for simple input normalization: `public string Email { get; } = Email?.Trim().ToLower();`.
   - For route parameters, use `[JsonIgnore] // Removes from API contract` on properties (including the comment).
   - Use default values for optional parameters where applicable, e.g., `int PageSize = 25`.
   - Use nullable reference types for optional parameters: e.g., `UserRole? UserRole = null`.
4. Response Record:
   - Create a public sealed record marked with `[PublicAPI]` for the response.
   - Name with `Response` suffix: e.g., `UserResponse`.
   - Include all necessary data for the client.
   - Use [Strongly Typed IDs](/.windsurf/rules/backend/strongly-typed-ids.md) and enums.
   - Take special care to not include sensitive data in the response.
5. Validator (optional):
   - Validation should focus on preventing malicious input like `PageSize=1_000_000_000`; the WebApp will typically ensure that the input is meaningful, so focus on malicious input.
   - Create a public sealed class with `Validator` suffix: e.g., `GetUsersQueryValidator`.
   - Each property should have one shared error message (e.g., "Search term must be no longer than 100 characters.").
   - Validation should only validate query properties (format, length, etc.), and should not make complex queries to e.g. a repository; use guards in the Query handler instead.
6. Handler:
   - Create a public sealed class with `Handler` suffix: e.g., `GetUsersHandler`.
   - Implement `IRequestHandler<QueryType, Result<ResponseType>>`.
   - Use guard statements with early returns that return [Result<T>](/application/shared-kernel/SharedKernel/Cqrs/Result.cs) instead of throwing exceptions.
   - If result messages contain values always enclose them in single quotes: `$"User with ID '{userId}' not found."`
   - Use repositories to retrieve data from the database, and never use Entity Framework directly.
   - Prefer using Mapster to map domain aggregates and entities to response DTOs. For complex mapping, map manually.
   - Queries should rarely track TelemetryEvents.
7. After changing the API, use the **execute MCP tool** with `command: "build"` for backend to generate the OpenAPI JSON contract. Then use the **execute MCP tool** with `command: "build"` for frontend to trigger `openapi-typescript` to generate the API contract used by the frontend.

Note: Queries run through MediatR pipeline behaviors in this order: Validation → Query → PublishTelemetryEvents

## Examples

```csharp
[PublicAPI]  // ✅ DO: Mark public API with [PublicAPI] and suffix with Query
public sealed record GetUsersQuery(string? Search = null, UserRole? UserRole = null, int PageOffset = 0, int PageSize = 25) 
    : IRequest<Result<UsersResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower(); // ✅ DO: Sanitize input
}

[PublicAPI]  // ✅ DO: Mark public API with [PublicAPI] and suffix with Response
public sealed record UsersResponse(int TotalCount, int PageSize, UserDetails[] Users);

[PublicAPI]
public sealed record UserDetails(UserId Id, string Email, UserRole Role);

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        // ✅ DO: Validate input
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
    }
}

public sealed class GetUsersHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, Result<UsersResponse>>
{
    public async Task<Result<UsersResponse>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        if (query.PageOffset >= totalPages)
        {
            // ✅ DO: Return Result<T> instead of throwing exceptions and enclose values in single quotes
            return Result<UsersResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var (users, count) = await userRepository.Search(query.Search, query.UserRole, query.PageOffset, query.PageSize, cancellationToken);

        var userResponses = users.Adapt<UserDetails[]>();  // ✅ DO: Use Mapster for simple cases
        return new UsersResponse(count, query.PageSize, userResponses);
    }
}
```

```csharp
[PublicAPI] // ❌ No Query suffix and don't use class instead of record
public sealed class BadUsers : IRequest<Result<BadUsersDto>>
{
    public bool UpdateLastAccessed { get; init; } = true; // ❌ Mutates state
}

// ❌ Don't use DTO suffix and don't skip the [PublicAPI] attribute
public sealed record BadUsersDto(UserId Id, string Email, UserRole Role);

public sealed class BadUsersHandler(IUserRepository userRepository)
    : IRequestHandler<BadUsers, Result<BadUsersDto>>
{
    public async Task<Result<BadUsersDto>> Handle(BadUsers query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException($"User with ID {query.UserId} not found"); // ❌ Throws exception, wrong message format
        }

        return new BadUsersDto(user.Id, user.Email, user.Role); // ❌ Manual mapping when Mapster can be used
    }
}
```