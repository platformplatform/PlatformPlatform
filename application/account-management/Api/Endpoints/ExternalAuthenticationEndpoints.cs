using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Commands;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class ExternalAuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/external-auth";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("ExternalAuthentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/login/start", async Task<ApiResult<StartExternalLoginResponse>> (StartExternalLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartExternalLoginResponse>().AllowAnonymous();

        group.MapGet("/login/callback/{provider}", async Task<ApiResult<string>> (string provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalLoginCommand(code, state, error, errorDescription) { Provider = provider })
        ).AllowAnonymous();

        group.MapPost("/signup/start", async Task<ApiResult<StartExternalSignupResponse>> (StartExternalSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartExternalSignupResponse>().AllowAnonymous();

        group.MapGet("/signup/callback/{provider}", async Task<ApiResult<string>> (string provider, string? code, string? state, string? error, [FromQuery(Name = "error_description")] string? errorDescription, IMediator mediator)
            => await mediator.Send(new CompleteExternalSignupCommand(code, state, error, errorDescription) { Provider = provider })
        ).AllowAnonymous();
    }
}
