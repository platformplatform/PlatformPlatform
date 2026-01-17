using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Commands;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class ExternalAuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("ExternalAuthentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/{provider}/login/start", async Task<ApiResult<StartExternalLoginResponse>> (ExternalProviderType provider, StartExternalLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { ProviderType = provider })
        ).Produces<StartExternalLoginResponse>().AllowAnonymous();

        group.MapGet("/{provider}/login/callback", async Task<ApiResult<string>> (ExternalProviderType provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalLoginCommand(code, state, error, errorDescription) { Provider = provider.ToString() })
        ).AllowAnonymous();

        group.MapPost("/{provider}/signup/start", async Task<ApiResult<StartExternalSignupResponse>> (ExternalProviderType provider, StartExternalSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { ProviderType = provider })
        ).Produces<StartExternalSignupResponse>().AllowAnonymous();

        group.MapGet("/{provider}/signup/callback", async Task<ApiResult<string>> (ExternalProviderType provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalSignupCommand(code, state, error, errorDescription) { Provider = provider.ToString() })
        ).AllowAnonymous();
    }
}
