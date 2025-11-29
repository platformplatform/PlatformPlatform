---
trigger: glob
globs: **/Queries/*.cs
description: Rules for CQRS queries, including structure, validation, response types, and mapping
---
# CQRS Queries

Carefully follow these instructions when implementing CQRS queries, including structure, validation, response types, and MediatR pipeline behaviors.

## Implementation

1. Create queries in `/[scs-name]/Core/Features/[Feature]/Queries`
2. Create one file per query containing Query, Response, Validator (optional), and Handler:
   - Name the file after the query without suffix (e.g., `GetUsers.cs`)
3. Query Record:
   - Create a public sealed record marked with `[PublicAPI]` that implements `IRequest<Result<TResponse>>`
   - Name with `Query` suffix (e.g., `GetUsersQuery`)
   - Define properties in the primary constructor
   - Use property initializers for input normalization: `public string Email { get; } = Email?.Trim().ToLower();`
   - For route parameters, use `[JsonIgnore] // Removes from API contract` on properties
   - Use default values for optional parameters (e.g., `int PageSize = 25`)
   - Use nullable reference types for optional parameters (e.g., `UserRole? UserRole = null`)
4. Response Record:
   - Create a public sealed record marked with `[PublicAPI]`
   - Name with `Response` suffix (e.g., `UserResponse`)
   - Include all necessary data for the client
   - Use [Strongly Typed IDs](/.agent/rules/backend/strongly-typed-ids.md) and enums
   - Take special care not to include sensitive data
5. Validator (optional):
   - Focus on preventing malicious input like `PageSize=1_000_000_000`—the WebApp ensures meaningful input
   - Create a public sealed class with `Validator` suffix (e.g., `GetUsersQueryValidator`)
   - Each property should have one shared error message
   - Only validate query properties (format, length)—use guards in the handler for complex checks
6. Handler:
   - Create a public sealed class with `Handler` suffix (e.g., `GetUsersHandler`)
   - Implement `IRequestHandler<QueryType, Result<ResponseType>>`
   - Use guard statements with early returns instead of throwing exceptions
   - Enclose dynamic values in single quotes: `$"User with ID '{userId}' not found."`
   - Use repositories to retrieve data—never use Entity Framework directly
   - Prefer Mapster for mapping; use manual mapping for complex cases
   - Never do N+1 operations—load all entities and process in memory
   - Queries should rarely track TelemetryEvents
7. After changing the API, run `build --backend` to generate the OpenAPI JSON contract, then `build --frontend` to trigger `openapi-typescript`

Note: Queries run through MediatR pipeline behaviors in this order: Validation → Query → PublishTelemetryEvents.

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
[PublicAPI] // ❌ No Query suffix, using class instead of record
public sealed class BadUsers : IRequest<Result<BadUsersDto>>
{
    public bool UpdateLastAccessed { get; init; } = true; // ❌ Queries must not mutate state
}

// ❌ Using DTO suffix, missing [PublicAPI] attribute
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

        
        if (someCondition)
        {
            return Result<BadUsersDto>.NotFound( // ❌ DON'T: Split Result returns across multiple lines if it fits on one line
                $"User with ID {query.UserId} not found" // ❌ Missing single quotes around dynamic value and trailing period
            );
        }

        return new BadUsersDto(user.Id, user.Email, user.Role); // ❌ Manual mapping when Mapster can be used
    }
}
```
