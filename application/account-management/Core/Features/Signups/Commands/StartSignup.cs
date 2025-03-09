using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Signups.Commands;

[PublicAPI]
public sealed record StartSignupCommand(string Email) : ICommand, IRequest<Result<StartSignupResponse>>
{
    public string Email { get; } = Email.Trim().ToLower();
}

[PublicAPI]
public sealed record StartSignupResponse(EmailConfirmationId EmailConfirmationId, int ValidForSeconds);

public sealed class StartSignupValidator : AbstractValidator<StartSignupCommand>
{
    public StartSignupValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartSignupHandler(IMediator mediator, ITelemetryEventsCollector events)
    : IRequestHandler<StartSignupCommand, Result<StartSignupResponse>>
{
    public async Task<Result<StartSignupResponse>> Handle(StartSignupCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new StartEmailConfirmationCommand(
                command.Email,
                "Confirm your email address",
                """
                <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
                <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
                <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
                """,
                EmailConfirmationType.Signup
            ),
            cancellationToken
        );

        if (!result.IsSuccess) return Result<StartSignupResponse>.From(result);

        events.CollectEvent(new SignupStarted());

        return Result<StartSignupResponse>.Success(new StartSignupResponse(result.Value!.EmailConfirmationId, EmailConfirmation.ValidForSeconds));
    }
}
