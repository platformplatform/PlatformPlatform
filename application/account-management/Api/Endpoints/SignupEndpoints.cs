using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Commands;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Signups.Commands;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Endpoints;

namespace PlatformPlatform.AccountManagement.Api.Endpoints;

public sealed class SignupEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/signups";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Signups").RequireAuthorization().ProducesValidationProblem();

        group.MapPost("/start", async Task<ApiResult<StartEmailSignupResponse>> (StartEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartEmailSignupResponse>().AllowAnonymous();

        group.MapPost("/{emailConfirmationId}/complete", async Task<ApiResult> (EmailConfirmationId emailConfirmationId, CompleteEmailSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { EmailConfirmationId = emailConfirmationId })
        ).AllowAnonymous();

        group.MapPost("/{emailConfirmationId}/resend-code", async Task<ApiResult<ResendEmailConfirmationCodeResponse>> (EmailConfirmationId emailConfirmationId, IMediator mediator)
            => await mediator.Send(new ResendEmailConfirmationCodeCommand { Id = emailConfirmationId })
        ).Produces<ResendEmailConfirmationCodeResponse>().AllowAnonymous();
    }
}
