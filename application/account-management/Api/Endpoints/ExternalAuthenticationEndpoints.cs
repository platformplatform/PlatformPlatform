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

        group.MapGet("/{provider}/login/start", async Task<ApiResult<string>> (ExternalProviderType provider, [AsParameters] StartExternalLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { ProviderType = provider })
        ).AllowAnonymous();

        group.MapGet("/{provider}/login/callback", async Task<ApiResult<string>> (ExternalProviderType provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalLoginCommand(code, state, error, errorDescription) { Provider = provider.ToString() })
        ).AllowAnonymous();

        group.MapGet("/{provider}/signup/start", async Task<ApiResult<string>> (ExternalProviderType provider, IMediator mediator)
            => await mediator.Send(new StartExternalSignupCommand { ProviderType = provider })
        ).AllowAnonymous();

        group.MapGet("/{provider}/signup/callback", async Task<ApiResult<string>> (ExternalProviderType provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalSignupCommand(code, state, error, errorDescription) { Provider = provider.ToString() })
        ).AllowAnonymous();
    }
}
