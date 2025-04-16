# CQRS Queries

When implementing a CQRS Query, follow these rules very carefully.

## Structure

Queries should be created in the `/[scs-name]/Core/Features/[Feature]/Queries` directory.

## Implementation

1. Create one file per query containing Query, Response, Validator, and Handler:
   - Name the file after the query without suffix: `GetUsers.cs`
2. Query Record:
   - Create a public sealed record marked with `[PublicAPI]` that implements `IRequest<Result<TResponse>>`.
   - Name with `Query` suffix: e.g. `GetUsersQuery`.
   - Define properties in the primary constructor.
   - Use property initializers for simple input normalization: `public string Search { get; } = Search?.Trim().ToLower();`.
   - For route parameters, use `[JsonIgnore] // Removes from API contract` on properties (including the comment).
   - Use default values for optional parameters where applicable, e.g. `int PageSize = 25`.
   - Use nullable reference types for optional parameters: e.g. `UserRole? UserRole = null`.
3. Response Record:
   - Create a public sealed record marked with `[PublicAPI]` for the response.
   - Name with `Response` suffix: `GetUsersResponse`.
   - Include all necessary data for the client.
   - Use strongly typed IDs and enums.
   - Take special care to not include sensitive data in the response.
4. Validator (optional):
   - Validation should focus on preventing malicious input like `PageSize=1_000_000_000`; the WebApp will typically ensure that the input is meaningful, so focus on malicious input.
   - Create public sealed class with `Validator` suffix: e.g. `GetUsersValidator`.
   - Each property should have one shared error message (e.g. "Search term must be no longer than 100 characters.").
   - Validation should only validate query properties (format, length, etc.)
5. Handler:
   - Create public sealed class with `Handler` suffix: e.g. `GetUsersHandler`.
   - Implement `IRequestHandler<QueryType, Result<ResponseType>>`.
   - Use guard statements with early returns that return `Result.XXX()` instead of throwing exceptions.
   - Use repository methods to retrieve data.
   - Map domain objects to response DTOs using Mapster.
   - Queries should rarely track TelemetryEvents.
6. After changing the API make sure to run `cd developer-cli && dotnet run build --backend` to generate the Open API JSON contract. Then run `cd developer-cli && dotnet run build --frontend` to trigger `openapi-typescript` to generate the API contract used by the frontend.

Queries run through MediatR pipeline behaviors in this order: Validation → Query → PublishTelemetryEvents

## Example 1 - Simple query

This example shows a simple query that retrieves a single tenant with no parameters.

```csharp
[PublicAPI]
public sealed record GetCurrentTenantQuery : IRequest<Result<TenantResponse>>;

[PublicAPI]
public sealed record TenantResponse(TenantId Id, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt, string Name, TenantState State);

public sealed class GetTenantHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetCurrentTenantQuery, Result<TenantResponse>>
{
    public async Task<Result<TenantResponse>> Handle(GetCurrentTenantQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        return tenant.Adapt<TenantResponse>();
    }
}
```

## Example 2 - Complex query with input validation and pagination

This example shows a query with multiple parameters, validation, and pagination.

```csharp
[PublicAPI]
public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    UserStatus? UserStatus = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int? PageOffset = null,
    int PageSize = 25
) : IRequest<Result<GetUsersResponse>>;

[PublicAPI]
public sealed record GetUsersResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, UserDetails[] Users);

[PublicAPI]
public sealed record UserDetails(
    UserId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    bool EmailConfirmed,
    string? AvatarUrl
);

public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
        RuleFor(x => x.PageSize).InclusiveBetween(0, 1000).WithMessage("The page size must be between 0 and 1000.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("The page offset must be greater than or equal to 0.");
    }
}

public sealed class GetUsersHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, Result<GetUsersResponse>>
{
    public async Task<Result<GetUsersResponse>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, count, totalPages) = await userRepository.Search(
            query.Search,
            query.UserRole,
            query.UserStatus,
            query.StartDate,
            query.EndDate,
            query.OrderBy,
            query.SortOrder,
            query.PageOffset,
            query.PageSize,
            cancellationToken
        );

        if (query.PageOffset.HasValue && query.PageOffset.Value >= totalPages)
        {
            return Result<GetUsersResponse>.BadRequest($"The page offset {query.PageOffset.Value} is greater than the total number of pages.");
        }

        var userResponses = users.Adapt<UserDetails[]>();
        return new GetUsersResponse(count, query.PageSize, totalPages, query.PageOffset ?? 0, userResponses);
    }
}
```