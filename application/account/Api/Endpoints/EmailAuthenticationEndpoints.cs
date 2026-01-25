using PlatformPlatform.Account.Features.EmailAuthentication.Commands;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.Account.Api.Endpoints;

public sealed class EmailAuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/authentication/email";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("EmailAuthentication").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/login/start", async Task<ApiResult<StartEmailLoginResponse>> (StartEmailLoginCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartEmailLoginResponse>().AllowAnonymous();

        group.MapPost("/login/{id}/complete", async Task<ApiResult> (EmailLoginId id, CompleteEmailLoginCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        ).AllowAnonymous();

        group.MapPost("/login/{emailConfirmationId}/resend-code", async Task<ApiResult<ResendEmailConfirmationCodeResponse>> (EmailConfirmationId emailConfirmationId, IMediator mediator)
            => await mediator.Send(new ResendEmailConfirmationCodeCommand { Id = emailConfirmationId })
        ).Produces<ResendEmailConfirmationCodeResponse>().AllowAnonymous();

        group.MapPost("/signup/start", async Task<ApiResult<StartEmailSignupResponse>> (StartEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartEmailSignupResponse>().AllowAnonymous();

        group.MapPost("/signup/{emailConfirmationId}/complete", async Task<ApiResult> (EmailConfirmationId emailConfirmationId, CompleteEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { EmailConfirmationId = emailConfirmationId })
        ).AllowAnonymous();

        group.MapPost("/signup/{emailConfirmationId}/resend-code", async Task<ApiResult<ResendEmailConfirmationCodeResponse>> (EmailConfirmationId emailConfirmationId, IMediator mediator)
            => await mediator.Send(new ResendEmailConfirmationCodeCommand { Id = emailConfirmationId })
        ).Produces<ResendEmailConfirmationCodeResponse>().AllowAnonymous();
    }
}
