# API Endpoints

When implementing API endpoints, follow these rules very carefully.

## Structure

API endpoints should be created in the `/application/[scs-name]/Api/Endpoints` directory, organized by feature area.

## Implementation

1. Create endpoint class implementing `IEndpoints` interface with proper naming (`[Feature]Endpoints.cs`).
2. Define a constant string for `RoutesPrefix`: `/api/[scs-name]/[feature]`:
   ```csharp
   private const string RoutesPrefix = "/api/account-management/users";
   ```
3. Set up route group with these configurations:
   ```csharp
   var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization().ProducesValidationProblem();
   ```
4. Structure each endpoint in exactly 3 lines (no logic in the body):
   - Line 1: Signature with route and parameters (don't break the line even if longer than 120 characters).
   - Line 2: Expression calling `=> mediator.Send()`.
   - Line 3: Optional configuration (`.Produces<T>()`, `.AllowAnonymous()`, etc.)
5. Follow these requirements:
   - Use strongly typed IDs for route parameters.
   - Return `ApiResult<T>` for queries and `ApiResult` or `IRequest<Result<T>>` for commands.
   - Use `[AsParameters]` for query parameters.
   - Use `with { Id = id }` syntax to bind route parameters to commands and queries.
6. After changing the API make sure to run `dotnet build` in the [application](/application) directory to generate the Open API JSON contract. Then run `npm run build` from the [application](/application) directory to trigger `openapi-typescript` to generate the API contract used by the frontend.
7. `IEndpoints` are automatically registered in the SharedKernel.


## Example 1 - User Endpoints

```csharp
public sealed class UserEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<GetUsersResponse>> ([AsParameters] GetUsersQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<GetUsersResponse>();

        group.MapDelete("/{id}", async Task<ApiResult> (UserId id, IMediator mediator)
            => await mediator.Send(new DeleteUserCommand(id))
        );

        group.MapPost("/bulk-delete", async Task<ApiResult> (BulkDeleteUsersCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        group.MapPut("/{id}/change-user-role", async Task<ApiResult> (UserId id, ChangeUserRoleCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );
        
        group.MapPost("/invite", async Task<ApiResult> (InviteUserCommand command, IMediator mediator)
            => await mediator.Send(command)
        );

        // The following endpoints are for the current user only
        group.MapGet("/me", async Task<ApiResult<UserResponse>> ([AsParameters] GetUserQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<UserResponse>()
    }
}
```

## Example 2 - Authentication Endpoints

```csharp
public sealed class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/login/start", async Task<ApiResult<StartLoginResponse>> (StartLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartLoginResponse>().AllowAnonymous();

        group.MapPost("login/{id}/complete", async Task<ApiResult> (LoginId id, CompleteLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();

        group.MapPost("login/{emailConfirmationId}/resend-code", async Task<ApiResult<ResendEmailConfirmationCodeResponse>> (EmailConfirmationId emailConfirmationId, IMediator mediator)
            => await mediator.Send(new ResendEmailConfirmationCodeCommand { Id = emailConfirmationId })
        ).Produces<ResendEmailConfirmationCodeResponse>().AllowAnonymous();

        group.MapPost("logout", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new LogoutCommand())
        );

        // Note: This endpoint must be called with the refresh token as Bearer token in the Authorization header
        routes.MapPost("/internal-api/account-management/authentication/refresh-authentication-tokens", async Task<ApiResult> (IMediator mediator)
            => await mediator.Send(new RefreshAuthenticationTokensCommand())
        );
    }
}
```

