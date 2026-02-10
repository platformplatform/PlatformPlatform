using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Commands;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class EmailAuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication/email";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("EmailAuthentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/login/start", async Task<ApiResult<StartEmailLoginResponse>> (StartEmailLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartEmailLoginResponse>().AllowAnonymous();

        group.MapPost("/login/{id}/complete", async Task<ApiResult> (EmailLoginId id, CompleteEmailLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();

        group.MapPost("/login/{id}/resend-code", async Task<ApiResult<ResendEmailLoginCodeResponse>> (EmailLoginId id, IMediator mediator)
            => await mediator.Send(new ResendEmailLoginCodeCommand { Id = id })
        ).Produces<ResendEmailLoginCodeResponse>().AllowAnonymous();

        group.MapPost("/signup/start", async Task<ApiResult<StartEmailSignupResponse>> (StartEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartEmailSignupResponse>().AllowAnonymous();

        group.MapPost("/signup/{id}/complete", async Task<ApiResult> (EmailLoginId id, CompleteEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { EmailLoginId = id })
        ).AllowAnonymous();

        group.MapPost("/signup/{id}/resend-code", async Task<ApiResult<ResendEmailLoginCodeResponse>> (EmailLoginId id, IMediator mediator)
            => await mediator.Send(new ResendEmailLoginCodeCommand { Id = id })
        ).Produces<ResendEmailLoginCodeResponse>().AllowAnonymous();
    }
}
