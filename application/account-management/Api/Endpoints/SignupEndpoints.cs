using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
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

        group.MapPost("/start", async Task<ApiResult<StartSignupResponse>> (StartSignupCommand command, IMediator mediator)
            => await mediator.Send(command)
        ).Produces<StartSignupResponse>().AllowAnonymous();

        group.MapPost("{emailConfirmationId}/complete", async Task<ApiResult> (EmailConfirmationId emailConfirmationId, CompleteSignupCommand command, IMediator mediator)
            => await mediator.Send(command with { EmailConfirmationId = emailConfirmationId })
        ).AllowAnonymous();

        group.MapPost("{emailConfirmationId}/resend-code", async Task<ApiResult<ResendEmailConfirmationCodeResponse>> (EmailConfirmationId emailConfirmationId, IMediator mediator)
            => await mediator.Send(new ResendEmailConfirmationCodeCommand { Id = emailConfirmationId })
        ).Produces<ResendEmailConfirmationCodeResponse>().AllowAnonymous();
    }
}
